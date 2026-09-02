import { Outlet } from "react-router-dom";
import { AlertTriangle } from "lucide-react";
import { AppSidebar } from "@/components/system/AppSidebar";
import { TopBar } from "@/components/system/TopBar";
import { useBackendUser } from "@/hooks/use-backend-user";

export default function Layout() {
  // Resolves the signed-in user against the API, which creates or refreshes their row.
  // A failure here must not lock anyone out, so it surfaces as a banner and nothing more.
  const { error } = useBackendUser();

  return (
    <div className="min-h-svh text-foreground">
      <AppSidebar />
      <div className="lg:pl-64 flex flex-col min-h-svh">
        <TopBar />
        {error && (
          <div className="mx-4 lg:mx-6 mt-3 rounded-xl border border-amber-300/70 bg-amber-50/80 px-3 py-2 flex items-start gap-2 text-xs text-amber-900">
            <AlertTriangle className="size-4 shrink-0 mt-px text-amber-600" />
            <span>
              Could not sync your profile with the backend: {error.message}
            </span>
          </div>
        )}
        <main className="flex-1 px-4 lg:px-6 py-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
