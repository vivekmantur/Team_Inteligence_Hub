import { useCallback, useMemo, useState } from "react";
import { BrowserAuthError, InteractionStatus } from "@azure/msal-browser";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import { isAuthConfigured, loginRequest, signUpRequest } from "@/lib/auth-config";
import { queryClient } from "@/lib/query-client";

export type CurrentUser = {
  name: string;
  email: string;
  initials: string;
  tenantId?: string;
  objectId?: string;
};

function toInitials(name: string, email: string): string {
  const source = name.trim() || email.trim();
  if (!source) return "?";
  const parts = source.replace(/[<>"]/g, "").split(/[\s.@_-]+/).filter(Boolean);
  const letters = parts.slice(0, 2).map((p) => p[0]);
  return (letters.join("") || source[0]).toUpperCase();
}

/**
 * Single entry point for Entra ID sign-in state and actions.
 *
 * Interactive calls use the popup flow and fall back to a full-page redirect when the
 * browser blocks popups.
 */
export function useAuth() {
  const { instance, inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  const account = instance.getActiveAccount() ?? instance.getAllAccounts()[0] ?? null;

  const user = useMemo<CurrentUser | null>(() => {
    if (!account) return null;
    const claims = (account.idTokenClaims ?? {}) as Record<string, unknown>;
    const name = account.name ?? (claims.name as string) ?? "";
    const email =
      account.username ||
      (claims.preferred_username as string) ||
      (claims.email as string) ||
      "";
    return {
      name: name || email,
      email,
      initials: toInitials(name, email),
      tenantId: account.tenantId,
      objectId: account.localAccountId,
    };
  }, [account]);

  const runInteractive = useCallback(
    async (request: typeof loginRequest) => {
      if (!isAuthConfigured) {
        setError(
          "Entra ID is not configured yet. Set VITE_ENTRA_CLIENT_ID and VITE_ENTRA_TENANT_ID in .env.local.",
        );
        return;
      }
      setError(null);
      setPending(true);
      // Optional dev-only override to force the redirect auth flow
      const forceRedirect = (import.meta.env.VITE_ENTRA_FORCE_REDIRECT || "").toString() === "true";
      if (forceRedirect) {
        try {
          await instance.loginRedirect(request);
        } finally {
          setPending(false);
        }
        return;
      }
      try {
        const result = await instance.loginPopup(request);
        if (result.account) instance.setActiveAccount(result.account);
      } catch (err) {
        // Popup blocked or closed by an embedded host — fall back to a redirect.
        if (
          err instanceof BrowserAuthError &&
          (err.errorCode === "popup_window_error" || err.errorCode === "empty_window_error")
        ) {
          await instance.loginRedirect(request);
          return;
        }
        if (err instanceof BrowserAuthError && err.errorCode === "user_cancelled") {
          setError(null);
        } else {
          setError(err instanceof Error ? err.message : "Sign-in failed. Please try again.");
        }
      } finally {
        setPending(false);
      }
    },
    [instance],
  );

  const signIn = useCallback(() => runInteractive(loginRequest), [runInteractive]);
  const signUp = useCallback(() => runInteractive(signUpRequest), [runInteractive]);

  const signOut = useCallback(async () => {
    setError(null);
    // Drop every cached response so the next user never sees the last one's data.
    queryClient.clear();
    try {
      await instance.logoutPopup({ account: account ?? undefined });
    } catch {
      await instance.logoutRedirect({ account: account ?? undefined });
    }
  }, [instance, account]);

  return {
    user,
    isAuthenticated: isAuthenticated && Boolean(account),
    isInteracting: pending || inProgress !== InteractionStatus.None,
    // Stay "not ready" while MSAL is still reading a redirect response, otherwise the
    // route guard bounces to /login before the account lands and the response is lost.
    isReady:
      inProgress !== InteractionStatus.Startup &&
      inProgress !== InteractionStatus.HandleRedirect,
    error,
    clearError: () => setError(null),
    signIn,
    signUp,
    signOut,
  };
}

export default useAuth;
