import { BrowserAuthError, InteractionRequiredAuthError } from "@azure/msal-browser";
import { apiScopes, msalInstance } from "./auth-config";

/** Base URL of TeamIntelligenceHub.API. Trailing slashes are trimmed so paths compose cleanly. */
export const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL?.trim() ?? "").replace(/\/+$/, "");

/** The backend can only be called once both the base URL and the API scope are set. */
export const isApiConfigured = apiBaseUrl.length > 0 && apiScopes.length > 0;

export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly body?: unknown,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

/**
 * True when this window was opened by another one, i.e. it is an MSAL popup.
 *
 * MSAL refuses to open a popup from inside a popup (`block_nested_popups`), so an
 * interactive fallback must never be attempted from one.
 */
function isInPopupWindow(): boolean {
  try {
    return typeof window !== "undefined" && !!window.opener && window.opener !== window;
  } catch {
    // Cross-origin opener — treat it as a popup and stay non-interactive.
    return true;
  }
}

/**
 * Returns an access token for the backend.
 *
 * Silent first — MSAL serves it from cache or refreshes it behind the scenes. Only when
 * Entra insists on interaction (consent, MFA, expired session) do we prompt, and only from
 * the top-level window.
 */
/**
 * Exported so multipart uploads can attach the token by hand — they cannot go through
 * apiFetch, which sets a JSON content type and would break the form boundary.
 */
export async function getAccessToken(): Promise<string> {
  if (!isApiConfigured) {
    throw new ApiError(
      0,
      "Backend is not configured. Set VITE_API_BASE_URL and VITE_ENTRA_API_SCOPE in .env.local.",
    );
  }

  const account = msalInstance.getActiveAccount() ?? msalInstance.getAllAccounts()[0];

  if (!account) {
    throw new ApiError(401, "No signed-in account.");
  }

  try {
    const result = await msalInstance.acquireTokenSilent({ scopes: apiScopes, account });
    return result.accessToken;
  } catch (error) {
    if (needsInteraction(error) && !isInPopupWindow()) {
      const result = await msalInstance.acquireTokenPopup({ scopes: apiScopes, account });
      return result.accessToken;
    }
    throw error;
  }
}

/**
 * Should a failed silent request be retried interactively?
 *
 * InteractionRequiredAuthError is the documented case. `timed_out` is the other one worth
 * catching: when no cached token can be refreshed, MSAL falls back to a hidden iframe and
 * waits `system.iframeBridgeTimeout` (10s) for the redirect page to broadcast the
 * response. Anything that stops the iframe reaching us — a slow load, an interstitial,
 * third-party cookies blocked so Entra never redirects back — surfaces as that timeout.
 * It is recoverable by asking the person, so it should not become a dead page.
 */
function needsInteraction(error: unknown): boolean {
  if (error instanceof InteractionRequiredAuthError) return true;

  return error instanceof BrowserAuthError && error.errorCode === "timed_out";
}

/**
 * Calls the backend with a bearer token attached.
 *
 * @param path Absolute path on the API, e.g. `/api/users/me`.
 */
export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = await getAccessToken();

  const headers = new Headers(init.headers);
  headers.set("Authorization", `Bearer ${token}`);
  headers.set("Accept", "application/json");
  if (init.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(`${apiBaseUrl}${path}`, { ...init, headers });

  if (!response.ok) {
    const body = await response.text();
    let parsed: unknown = body;
    try {
      parsed = body ? JSON.parse(body) : undefined;
    } catch {
      // Non-JSON error body — keep the raw text.
    }

    // Surface the server's own explanation. Without it a 401 from a missing claim
    // looks identical to a 401 from a rejected token.
    const detail =
      typeof parsed === "object" && parsed !== null && "message" in parsed
        ? String((parsed as { message: unknown }).message)
        : typeof parsed === "string" && parsed.trim()
          ? parsed.trim()
          : "";

    throw new ApiError(
      response.status,
      `${response.status} ${response.statusText} — ${path}${detail ? `: ${detail}` : ""}`,
      parsed,
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
