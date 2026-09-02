import { useEffect, useState, type PropsWithChildren } from "react";
import { EventType, type AuthenticationResult, type EventMessage } from "@azure/msal-browser";
import { MsalProvider } from "@azure/msal-react";
import { msalInstance } from "@/lib/auth-config";

/**
 * Wraps the app in MSAL and keeps a single active account in sync.
 *
 * MsalProvider handles `initialize()` and `handleRedirectPromise()` internally, so we
 * only register the event callback that promotes a fresh login to the active account.
 */
export function AuthProvider({ children }: PropsWithChildren) {
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const existing = msalInstance.getActiveAccount();
    if (!existing) {
      const [first] = msalInstance.getAllAccounts();
      if (first) msalInstance.setActiveAccount(first);
    }

    const callbackId = msalInstance.addEventCallback((event: EventMessage) => {
      if (
        (event.eventType === EventType.LOGIN_SUCCESS ||
          event.eventType === EventType.ACQUIRE_TOKEN_SUCCESS) &&
        event.payload
      ) {
        const { account } = event.payload as AuthenticationResult;
        if (account) msalInstance.setActiveAccount(account);
      }

      if (event.eventType === EventType.LOGOUT_SUCCESS) {
        msalInstance.setActiveAccount(null);
      }
    });

    setReady(true);

    return () => {
      if (callbackId) msalInstance.removeEventCallback(callbackId);
    };
  }, []);

  if (!ready) return null;

  return <MsalProvider instance={msalInstance}>{children}</MsalProvider>;
}

export default AuthProvider;
