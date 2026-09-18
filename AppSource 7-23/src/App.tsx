import { Suspense } from "react";
import { BrowserRouter as Router, Routes, Route, Navigate } from "react-router-dom";
import { QueryClientProvider } from "@tanstack/react-query";
import Layout from "./pages/_layout";
import { queryClient } from "./lib/query-client";
import { AppProviders } from "@/components/system/AppProviders";
import { AuthProvider } from "@/components/system/AuthProvider";
import { RequireAuth } from "@/components/system/RequireAuth";
import LoginPage from "./pages/login";
import HomePage from "./pages/index";
import InsightsPage from "./pages/insights";
import InitiativesPage from "./pages/initiatives";
import NewInitiativePage from "./pages/initiatives-new";
import InitiativeDetailPage from "./pages/initiative-detail";
import TeamPage from "./pages/team";
import StoriesPage from "./pages/stories";
import ContentStudioPage from "./pages/content-studio";
import KnowledgePage from "./pages/knowledge";
import CopilotPage from "./pages/copilot";
import AboutTeamPage from "./pages/about-team";
import NotFoundPage from "./pages/not-found";
import { AppErrorBoundary } from "./components/system/AppErrorBoundary";

// NOTE(ai): DO NOT REMOVE — dev-mode detection for router basename logic.
const isDevMode = import.meta.env.DEV;

// NOTE(ai): DO NOT REMOVE — (ROUTER BASE) keep this or deep links break in playback.
function getBase(pathname: string): string {
  const parts = pathname.split("/").filter(Boolean);
  return parts.length ? `/${parts[0]}/` : "/";
}
// NOTE(ai): DO NOT REMOVE - used by Router
let appNameBase: string | undefined = undefined;
if (!isDevMode) {
  appNameBase = getBase(window.location.pathname);
}

function App() {
  return (
    <AuthProvider>
      {/* Above AppProviders: ContributionProvider and InitiativeProvider both run
          queries, so the client has to exist before they mount. */}
      <QueryClientProvider client={queryClient}>
        <AppProviders>
          {/* NOTE(ai): DO NOT REMOVE - Single router lives here. */}
          <Router basename={appNameBase}>
            <AppErrorBoundary>
              <Suspense fallback={<div className="p-4 text-sm text-muted-foreground">Loading…</div>}>
                <Routes>
                {/* Public landing page — Microsoft Entra ID sign-in */}
                <Route path="/login" element={<LoginPage />} />

                <Route
                  path="/"
                  element={
                    <RequireAuth>
                      <Layout />
                    </RequireAuth>
                  }
                >
                  <Route index element={<HomePage />} />
                  <Route path="analytics" element={<InsightsPage />} />
                  <Route path="initiatives" element={<InitiativesPage />} />
                  <Route path="initiatives/new" element={<NewInitiativePage />} />
                  <Route path="initiatives/:id/edit" element={<NewInitiativePage />} />
                  <Route path="initiatives/:id" element={<InitiativeDetailPage />} />
                  {/* Backward-compat redirects from legacy /projects routes */}
                  <Route path="projects" element={<Navigate to="/initiatives" replace />} />
                  <Route path="projects/new" element={<Navigate to="/initiatives/new" replace />} />
                  <Route path="projects/:id" element={<Navigate to="/initiatives" replace />} />
                  <Route path="team" element={<TeamPage />} />
                  <Route path="stories" element={<StoriesPage />} />
                  {/* Customer Zero and Testimonials were merged into the unified
                      Stories & Evidence page — redirect old bookmarks/links. */}
                  <Route path="customer-zero" element={<Navigate to="/stories" replace />} />
                  <Route path="testimonials" element={<Navigate to="/stories" replace />} />
                  {/* Role Hub Analytics and Agent Analytics were folded into the
                      Insights tabs on /analytics — redirect old bookmarks/links. */}
                  <Route path="role-hub" element={<Navigate to="/analytics" replace />} />
                  <Route path="agents" element={<Navigate to="/analytics" replace />} />
                  <Route path="content-studio" element={<ContentStudioPage />} />
                  <Route path="knowledge" element={<KnowledgePage />} />
                  <Route path="copilot" element={<CopilotPage />} />
                  <Route path="aboutteam" element={<AboutTeamPage />} />
                  {/* Old Administration route — replaced by /aboutteam. */}
                  <Route path="admin" element={<Navigate to="/aboutteam" replace />} />
                  {/* NOTE(ai): DO NOT REMOVE — catch-all 404 page */}
                  <Route path="*" element={<NotFoundPage />} />
                </Route>
                </Routes>
              </Suspense>
            </AppErrorBoundary>
          </Router>
        </AppProviders>
      </QueryClientProvider>
    </AuthProvider>
  );
}

export default App;
