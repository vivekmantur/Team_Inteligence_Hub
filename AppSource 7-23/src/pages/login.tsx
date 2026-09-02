import { useEffect } from "react";
import { Navigate, useLocation } from "react-router-dom";
import {
  Sparkles,
  ShieldCheck,
  BarChart3,
  BookOpen,
  Users,
  AlertTriangle,
  ArrowRight,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { useAuth } from "@/hooks/use-auth";
import { isAuthConfigured, isSignUpEnabled } from "@/lib/auth-config";

/** Microsoft's four-square mark, used on the sign-in button per Microsoft branding guidance. */
function MicrosoftLogo({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 23 23" aria-hidden="true" className={className}>
      <rect x="1" y="1" width="10" height="10" fill="#F25022" />
      <rect x="12" y="1" width="10" height="10" fill="#7FBA00" />
      <rect x="1" y="12" width="10" height="10" fill="#00A4EF" />
      <rect x="12" y="12" width="10" height="10" fill="#FFB900" />
    </svg>
  );
}

const highlights = [
  {
    icon: BarChart3,
    title: "Executive-ready insight",
    body: "Turn initiative activity into QBR slides, briefs, and metrics without rebuilding the story each quarter.",
  },
  {
    icon: BookOpen,
    title: "Knowledge that compounds",
    body: "Case studies, testimonials, and lessons learned stay searchable instead of scattered across chats and decks.",
  },
  {
    icon: Users,
    title: "One view of the team",
    body: "Contributions, capacity, and customer-zero adoption in a single hub your whole team works from.",
  },
];

export default function LoginPage() {
  const location = useLocation();
  const { isAuthenticated, isReady, isInteracting, error, clearError, signIn, signUp } = useAuth();

  const from = (location.state as { from?: string } | null)?.from ?? "/";

  useEffect(() => {
    clearError();
    // Intentionally runs once on mount to clear any stale error from a prior attempt.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  if (isReady && isAuthenticated) {
    return <Navigate to={from} replace />;
  }

  return (
    <div className="relative min-h-svh overflow-hidden">
      <div className="absolute inset-0 bg-mesh pointer-events-none" />
      <div className="absolute -top-32 -left-24 size-96 rounded-full bg-copilot-gradient opacity-25 blur-3xl animate-float-slow pointer-events-none" />
      <div className="absolute -bottom-40 -right-24 size-96 rounded-full bg-copilot-gradient opacity-20 blur-3xl pointer-events-none" />

      <div className="relative mx-auto max-w-6xl px-4 lg:px-6 py-10 lg:py-16">
        <div className="grid lg:grid-cols-[1.1fr_minmax(0,420px)] gap-10 lg:gap-14 items-center">
          {/* Story panel */}
          <section>
            <div className="inline-flex items-center gap-2 text-[11px] font-semibold uppercase tracking-[0.14em] text-gradient">
              <Sparkles className="size-3.5 animate-sparkle" />
              Strategic execution & change management platform
            </div>
            <h1 className="mt-4 text-4xl md:text-5xl font-semibold tracking-tight leading-[1.05]">
              <span className="text-gradient">Team Intelligence Hub</span>
            </h1>
            <p className="mt-4 max-w-xl text-base md:text-lg text-muted-foreground">
              Sign in with your work account to see initiative health, contribution activity, and
              the content your team is shipping this quarter.
            </p>

            <ul className="mt-8 grid sm:grid-cols-2 lg:grid-cols-1 gap-3 max-w-xl">
              {highlights.map(({ icon: Icon, title, body }) => (
                <li key={title} className="glass rounded-2xl p-4 flex gap-3">
                  <span className="size-9 shrink-0 rounded-xl bg-copilot-gradient grid place-items-center text-white shadow">
                    <Icon className="size-4" />
                  </span>
                  <div>
                    <p className="text-sm font-medium">{title}</p>
                    <p className="text-xs text-muted-foreground mt-0.5">{body}</p>
                  </div>
                </li>
              ))}
            </ul>
          </section>

          {/* Sign-in card */}
          <section className="glass-strong rounded-3xl p-6 md:p-8">
            <div className="flex items-center gap-2 text-xs font-medium text-muted-foreground">
              <ShieldCheck className="size-4 text-emerald-600" />
              Secured by Microsoft Entra ID
            </div>

            <h2 className="mt-4 text-2xl font-semibold tracking-tight">Welcome</h2>
            <p className="mt-1.5 text-sm text-muted-foreground">
              Use your organization account. We never see or store your password.
            </p>

            {!isAuthConfigured && (
              <Alert className="mt-5 border-amber-300/70 bg-amber-50/80">
                <AlertTriangle className="size-4 text-amber-600" />
                <AlertTitle className="text-amber-900">Entra ID not configured</AlertTitle>
                <AlertDescription className="text-amber-900/80">
                  Add <code className="font-mono text-[11px]">VITE_ENTRA_CLIENT_ID</code> and{" "}
                  <code className="font-mono text-[11px]">VITE_ENTRA_TENANT_ID</code> to{" "}
                  <code className="font-mono text-[11px]">.env.local</code>, then restart the dev
                  server.
                </AlertDescription>
              </Alert>
            )}

            {error && (
              <Alert className="mt-5 border-rose-300/70 bg-rose-50/80">
                <AlertTriangle className="size-4 text-rose-600" />
                <AlertTitle className="text-rose-900">Sign-in failed</AlertTitle>
                <AlertDescription className="text-rose-900/80 break-words">{error}</AlertDescription>
              </Alert>
            )}

            <div className="mt-6 space-y-3">
              <Button
                onClick={signIn}
                disabled={isInteracting}
                className="w-full h-11 rounded-xl bg-white text-foreground border border-black/10 shadow-sm hover:bg-white/90"
              >
                {isInteracting ? (
                  <span className="size-4 rounded-full border-2 border-current border-t-transparent animate-spin" />
                ) : (
                  <MicrosoftLogo className="size-4" />
                )}
                <span className="font-medium">Sign in with Microsoft</span>
              </Button>

              {isSignUpEnabled && (
                <Button
                  variant="outline"
                  onClick={signUp}
                  disabled={isInteracting}
                  className="w-full h-11 rounded-xl bg-white/60"
                >
                  Create an account
                  <ArrowRight className="size-4" />
                </Button>
              )}
            </div>

            <p className="mt-5 text-xs text-muted-foreground leading-relaxed">
              {isSignUpEnabled ? (
                <>
                  New here? Choose <span className="font-medium">Create an account</span> to
                  register, then come back and sign in.
                </>
              ) : (
                <>
                  Accounts are provisioned by your organization. If sign-in is blocked, ask your
                  Microsoft 365 administrator to grant you access to Team Intelligence Hub.
                </>
              )}
            </p>

            <p className="mt-4 pt-4 border-t border-black/5 text-[11px] text-muted-foreground">
              By signing in you agree to your organization's acceptable use policy.
            </p>
          </section>
        </div>
      </div>
    </div>
  );
}
