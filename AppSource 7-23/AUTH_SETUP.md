# Microsoft Entra ID sign-in

The app opens on a login landing page at `/login`. Every other route is behind a guard that
sends signed-out visitors there. Sign-in uses MSAL with the authorization code flow + PKCE, so
no client secret lives in the frontend.

## What was added

| File | Purpose |
| --- | --- |
| `src/lib/auth-config.ts` | MSAL configuration, scopes, and the shared `PublicClientApplication` |
| `src/components/system/AuthProvider.tsx` | Wraps the app in `MsalProvider`, keeps the active account in sync |
| `src/components/system/RequireAuth.tsx` | Route guard; redirects to `/login` and remembers the target route |
| `src/hooks/use-auth.ts` | `signIn`, `signUp`, `signOut`, and the normalized current user |
| `src/pages/login.tsx` | The landing page |
| `src/components/system/UserMenu.tsx` | Top-bar avatar with the signed-in identity and sign out |
| `src/main.tsx` | Hands the `/authorize` response to MSAL's redirect bridge before React renders |

## How the redirect response is handled

MSAL v5 posts the `/authorize` response back to the redirect URI, which is this app. The page that
loads there has to hand the response to the window that started sign-in — over `BroadcastChannel`
for popups, or via `sessionStorage` plus a navigation for the redirect flow.

`src/main.tsx` checks for an auth response in the URL and calls `broadcastResponseToMainFrame()`
from `@azure/msal-browser/redirect-bridge` instead of mounting React. If React mounted first, the
route guard would navigate to `/login` and wipe the response out of the URL, leaving the popup
parked on the login page. Anything that is not a valid response falls through to the normal UI.

Keep this ahead of `createRoot(...)` if you refactor the entry point.

## 1. Register the app in Entra ID

1. Go to **Microsoft Entra admin center → App registrations → New registration**.
2. Name it `Team Intelligence Hub`.
3. Supported account types: **Accounts in this organizational directory only** (single tenant).
4. Redirect URI: platform **Single-page application (SPA)**, value `http://localhost:5173/`.
5. Register, then copy the **Application (client) ID** and **Directory (tenant) ID**.

Add a second SPA redirect URI for each environment you deploy to. For the Power Apps code app,
that is the published app URL including its trailing slash, for example
`https://apps.powerapps.com/play/e/<env>/a/<appId>/`.

Under **API permissions**, the delegated Microsoft Graph `User.Read` permission is present by
default. That is all the login page needs.

## 2. Configure the frontend

```bash
cp .env.example .env.local
```

Fill in:

```
VITE_ENTRA_CLIENT_ID=<application-client-id>
VITE_ENTRA_TENANT_ID=<directory-tenant-id>
```

Restart the dev server. Vite only reads env vars at startup.

Until `VITE_ENTRA_CLIENT_ID` is set, the login page renders a setup notice instead of failing on
click, so the page is safe to open before the registration exists.

## 3. Sign-up

Entra ID workforce tenants have no self-service sign-up. Accounts are provisioned by an admin,
and the login page says so.

If you use **Entra External ID** (the CIAM tenant type) with a sign-up user flow, set:

```
VITE_ENTRA_SIGNUP_ENABLED=true
VITE_ENTRA_AUTHORITY=https://<tenant-name>.ciamlogin.com/<tenant-id>
```

That reveals the **Create an account** button, which calls the same authorize endpoint with
`prompt=create`.

## Behavior notes

- Sign-in uses a popup and falls back to a full-page redirect when the browser blocks popups.
- Tokens are cached in `sessionStorage`. Switch `cacheLocation` to `localStorage` in
  `src/lib/auth-config.ts` if you want sign-in to persist across tabs and restarts.
- Deep links survive sign-in: `/analytics` while signed out lands on `/login` and returns to
  `/analytics` afterward.
- `useAuth()` exposes `user.name`, `user.email`, `user.initials`, `user.objectId`, and
  `user.tenantId` for any component that needs the current identity.

## Calling an API with the token

```ts
import { msalInstance, loginRequest } from "@/lib/auth-config";

const result = await msalInstance.acquireTokenSilent({
  ...loginRequest,
  account: msalInstance.getActiveAccount()!,
});
// result.accessToken -> Authorization: Bearer <token>
```

## Power Apps note

This project is also packaged as a Power Apps code app (`power.config.json`). When it runs inside
the Power Apps player, the host has already authenticated the user against the same tenant, so the
login page will pass through after one silent sign-in rather than prompting again. Standalone
hosting is where the landing page does the real work.
