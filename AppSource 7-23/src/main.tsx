import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { broadcastResponseToMainFrame } from "@azure/msal-browser/redirect-bridge";
import "./index.css";


/* NOTE(ai): DO NOT REMOVE — Normalize share-link path before React Router
   Pattern: "/<id>/index.html" to "/<id>/"
   Reason: Router basename uses window.location.pathname and must point to the directory root
   When:   Run before first render; uses history.replaceState (no network redirect) */
if (window.location.pathname.endsWith('/index.html')) {
  const newPath = window.location.pathname.replace('/index.html', '/');
  window.history.replaceState(null, '', newPath + window.location.search + window.location.hash);
}

/* NOTE(ai): DO NOT REMOVE — Entra ID redirect-bridge handoff, must run before React renders.
   Why:  MSAL v5 sends the /authorize response back to the redirect URI, which is this app.
         The page loaded there has to hand the response to the window that started sign-in
         (BroadcastChannel for popups, sessionStorage + navigation for the redirect flow).
         If React boots first, the auth guard navigates to /login and wipes the response from
         the URL, so the popup just sits there showing the login page and never signs in.
   How:  Detect a bridge response in the hash or query and let MSAL take the window. It
         closes the popup (or navigates back) on success, so we only render on failure.
   Note: `state` alone is the test, matching the bridge's own contract. Sign-out responses
         carry state without a code, and they need the handoff just as much -- otherwise the
         popup renders the app and the main window waits forever for a response. */
function hasMsalResponse(): boolean {
  const sources = [
    window.location.hash.replace(/^#\/?/, ""),
    window.location.search.replace(/^\?/, ""),
  ];
  return sources.some((raw) => Boolean(raw) && new URLSearchParams(raw).has("state"));
}

/* App is imported dynamically, not at the top of this file.
   Why:  MSAL's silent-renewal iframe loads this same page. A static import would make
         the iframe evaluate the whole module graph first -- including auth-config.ts,
         which constructs a second PublicClientApplication in the same origin. MSAL warns
         about that ("There is already an instance of MSAL.js in the window with the same
         client id") and the duplicate races the bridge for the response, so the handoff
         never happens and acquireTokenSilent fails with timed_out after 10s.
   How:  Keeping it dynamic means the bridge path below imports nothing but MSAL. The app
         is only pulled in when this page is actually the app rather than a redirect. */
async function renderApp() {
  const { default: App } = await import("./App.tsx");

  createRoot(document.getElementById("root")!).render(
    <StrictMode>
        <App />
    </StrictMode>
  );
}

if (hasMsalResponse()) {
  const originalTitle = document.title;
  broadcastResponseToMainFrame().catch((error) => {
    // Not a response this app can process — fall back to the normal UI.
    console.error("[msal] could not process the redirect response", error);
    document.title = originalTitle;
    void renderApp();
  });
} else {
  void renderApp();
}
