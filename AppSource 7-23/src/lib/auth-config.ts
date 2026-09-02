import {
  LogLevel,
  PublicClientApplication,
  type Configuration,
  type RedirectRequest,
} from "@azure/msal-browser";

/**
 * Microsoft Entra ID configuration.
 *
 * Values come from Vite env vars so the same build can target different tenants.
 * Copy `.env.example` to `.env.local` and fill in the app registration details.
 */
const clientId = import.meta.env.VITE_ENTRA_CLIENT_ID?.trim() ?? "";
const tenantId = import.meta.env.VITE_ENTRA_TENANT_ID?.trim() || "organizations";
const authorityOverride = import.meta.env.VITE_ENTRA_AUTHORITY?.trim() ?? "";

/** True once a client ID is present. The login page shows setup guidance when false. */
export const isAuthConfigured = clientId.length > 0;

/** Self-service sign-up requires Entra External ID (or an approved sign-up user flow). */
export const isSignUpEnabled =
  import.meta.env.VITE_ENTRA_SIGNUP_ENABLED?.trim().toLowerCase() === "true";

/**
 * Redirect URI must be registered in the app registration under "Single-page application".
 * Defaults to the directory the app is served from, which keeps deep links and the
 * Power Apps `/<appId>/` base path working.
 */
function defaultRedirectUri(): string {
  if (typeof window === "undefined") return "";
  const base = window.location.pathname.endsWith("/")
    ? window.location.pathname
    : `${window.location.pathname.replace(/[^/]*$/, "")}`;
  return `${window.location.origin}${base}`;
}

const redirectUri = import.meta.env.VITE_ENTRA_REDIRECT_URI?.trim() || defaultRedirectUri();

export const msalConfig: Configuration = {
  auth: {
    clientId,
    authority: authorityOverride || `https://login.microsoftonline.com/${tenantId}`,
    redirectUri,
    postLogoutRedirectUri: redirectUri,
  },
  cache: {
    // sessionStorage keeps tokens scoped to the tab; switch to "localStorage" for
    // cross-tab single sign-on.
    cacheLocation: "sessionStorage",
  },
  system: {
    loggerOptions: {
      // Use verbose logging during development to capture MSAL internals.
      logLevel: import.meta.env.DEV ? LogLevel.Verbose : LogLevel.Error,
      piiLoggingEnabled: false,
      // logLevel above asks for Verbose in dev, so the callback has to print below
      // Warning too. Dropping it meant the interesting part of a silent-auth failure
      // (which flow ran, whether the iframe was used) never reached the console.
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) return;
        if (level === LogLevel.Error) console.error("[msal]", message);
        else if (!import.meta.env.DEV) return;
        else if (level === LogLevel.Warning) console.warn("[msal]", message);
        else console.debug("[msal]", message);
      },
    },
  },
};

/** The API scope this app calls its own backend with. Empty when unconfigured. */
export const apiScope = import.meta.env.VITE_ENTRA_API_SCOPE?.trim() ?? "";

/** Scope set for backend calls. Resource scopes are requested on their own, never
 *  mixed with the OIDC scopes, so the token comes back for the right audience. */
export const apiScopes = apiScope ? [apiScope] : [];

/** Delegated scopes requested at sign-in. Add Graph or API scopes here as needed. */
export const loginRequest: RedirectRequest = {
  scopes: ["openid", "profile", "email", ...apiScopes],
};

/**
 * Sign-up uses the same authorize endpoint with `prompt=create`, which Entra honors
 * when self-service sign-up is enabled on the tenant (Entra External ID).
 */
export const signUpRequest: RedirectRequest = {
  ...loginRequest,
  prompt: "create",
};

export const msalInstance = new PublicClientApplication(msalConfig);

// Expose for debugging in dev so you can call methods from DevTools (e.g. msalInstance.loginRedirect())
if (typeof window !== "undefined") {
  try {
    // eslint-disable-next-line @typescript-eslint/ban-ts-comment
    // @ts-ignore - attach for debugging only
    window.msalInstance = msalInstance;
  } catch {
    // Debug convenience only — ignore environments that block window writes.
  }
}
