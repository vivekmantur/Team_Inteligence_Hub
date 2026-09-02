import type { PropsWithChildren } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "@/hooks/use-auth";

/**
 * Route guard. Sends signed-out visitors to the login landing page and remembers
 * where they were headed so sign-in can return them there.
 */
export function RequireAuth({ children }: PropsWithChildren) {
  const { isAuthenticated, isReady } = useAuth();
  const location = useLocation();

  if (!isReady) {
    return (
      <div className="min-h-svh grid place-items-center">
        <div className="flex items-center gap-3 text-sm text-muted-foreground">
          <span className="size-4 rounded-full border-2 border-current border-t-transparent animate-spin" />
          Checking your sign-in…
        </div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />;
  }

  return <>{children}</>;
}

export default RequireAuth;
