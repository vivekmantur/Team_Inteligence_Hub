import { useNavigate } from "react-router-dom";
import {
  Plus,
  Sparkles,
  Users,
  CalendarClock,
  AlertTriangle,
  ListChecks,
  Rocket,
  Wand2,
  Presentation,
  ArrowUpRight,
} from "lucide-react";
import { PageHeader } from "@/components/system/PageHeader";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { useAddContribution } from "@/components/contribution/ContributionContext";
import {
  attentionStats,
  thisWeekSignals,
  myInitiatives,
  recentActivity,
  attentionHighlights,
  upcomingDeadlines,
  type WeeklySignalSeverity,
} from "@/data/mock";

const statIcons = {
  capacity: Users,
  reports: CalendarClock,
  risks: AlertTriangle,
  actions: ListChecks,
} as const;

const statAccents = {
  capacity: "bg-indigo-500/10 text-indigo-600",
  reports: "bg-fuchsia-500/10 text-fuchsia-600",
  risks: "bg-red-500/10 text-red-600",
  actions: "bg-violet-500/10 text-violet-600",
} as const;

const severityAccents: Record<WeeklySignalSeverity, string> = {
  risk: "bg-red-500/10 text-red-600",
  review: "bg-indigo-500/10 text-indigo-600",
  checkpoint: "bg-amber-500/10 text-amber-600",
  new: "bg-fuchsia-500/10 text-fuchsia-600",
};

const statusBadge: Record<string, string> = {
  "On Track": "bg-emerald-500/10 text-emerald-700",
  "At Risk": "bg-rose-500/10 text-rose-700",
  "Needs Attention": "bg-amber-500/10 text-amber-700",
};

export default function HomePage() {
  const navigate = useNavigate();
  const { openAddContribution } = useAddContribution();

  const quickActions = [
    { label: "Review my Initiatives", icon: Rocket, onClick: () => navigate("/initiatives") },
    { label: "Generate grounded content", icon: Wand2, onClick: () => navigate("/content-studio") },
    { label: "Capture a contribution", icon: Sparkles, onClick: () => openAddContribution() },
    { label: "Review leadership reporting", icon: Presentation, onClick: () => navigate("/content-studio?type=Executive%20Summary") },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="My Workspace"
        title="My Attention"
        description="A personalized view of the Initiatives, actions, risks, deadlines and updates that need your attention."
        actions={
          <>
            <Button
              variant="outline"
              className="rounded-xl bg-white/70"
              onClick={() => navigate("/initiatives/new")}
            >
              <Plus className="size-4" /> New Initiative
            </Button>
            <Button
              onClick={() => openAddContribution()}
              className="rounded-xl bg-copilot-gradient text-white"
            >
              <Sparkles className="size-4" /> Add Contribution
            </Button>
          </>
        }
      />

      {/* Attention stats */}
      <section className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {attentionStats.map((s) => {
          const Icon = statIcons[s.id as keyof typeof statIcons];
          return (
            <div key={s.id} className="glass rounded-2xl p-4">
              <div className={cn("size-9 rounded-xl grid place-items-center", statAccents[s.id as keyof typeof statAccents])}>
                <Icon className="size-4" />
              </div>
              <div className="mt-3 text-2xl font-semibold tracking-tight">{s.value}</div>
              <div className="text-[13px] font-medium mt-0.5">{s.label}</div>
              <div className="text-[11px] text-muted-foreground mt-1">{s.caption}</div>
            </div>
          );
        })}
      </section>

      {/* This Week + Quick Actions */}
      <section className="grid lg:grid-cols-3 gap-4">
        <div className="glass rounded-2xl p-5 lg:col-span-2">
          <div className="flex items-start justify-between">
            <div>
              <h3 className="font-semibold">This Week</h3>
              <p className="text-xs text-muted-foreground mt-0.5">
                Prioritized signals from your mock assignments and Initiative state.
              </p>
            </div>
            <span className="text-[11px] font-semibold px-2.5 py-1 rounded-full bg-indigo-500/10 text-indigo-700 shrink-0">
              {thisWeekSignals.length} items
            </span>
          </div>
          <ul className="mt-4 divide-y divide-black/5">
            {thisWeekSignals.map((sig) => (
              <li key={sig.id} className="py-3 flex items-start gap-3">
                <div className={cn("size-8 rounded-lg grid place-items-center shrink-0 mt-0.5", severityAccents[sig.severity])}>
                  <AlertTriangle className="size-4" />
                </div>
                <div className="flex-1 min-w-0">
                  <div className="text-sm font-medium">{sig.title}</div>
                  <div className="text-[12px] text-muted-foreground mt-0.5">{sig.description}</div>
                </div>
                <span className="text-[11px] text-muted-foreground shrink-0 mt-0.5">{sig.when}</span>
              </li>
            ))}
          </ul>
        </div>

        <div className="glass rounded-2xl p-5">
          <h3 className="font-semibold">Quick Actions</h3>
          <p className="text-xs text-muted-foreground mt-0.5">
            Common actions across the Team Intelligence flow.
          </p>
          <ul className="mt-4 space-y-1">
            {quickActions.map((a) => {
              const Icon = a.icon;
              return (
                <li key={a.label}>
                  <button
                    onClick={a.onClick}
                    className="w-full flex items-center gap-3 rounded-xl px-2 py-2.5 hover:bg-white/60 transition text-left"
                  >
                    <div className="size-9 rounded-xl grid place-items-center bg-gradient-to-br from-indigo-500/15 to-fuchsia-500/15 text-indigo-600 shrink-0">
                      <Icon className="size-4" />
                    </div>
                    <span className="flex-1 text-sm font-medium">{a.label}</span>
                    <ArrowUpRight className="size-4 text-muted-foreground" />
                  </button>
                </li>
              );
            })}
          </ul>
        </div>
      </section>

      {/* My Initiatives + Recent Activity */}
      <section className="grid lg:grid-cols-3 gap-4">
        <div className="glass rounded-2xl p-5 lg:col-span-2">
          <div className="flex items-center justify-between">
            <div>
              <h3 className="font-semibold">My Initiatives</h3>
              <p className="text-xs text-muted-foreground mt-0.5">
                Owned by you or where you are on the Initiative team.
              </p>
            </div>
            <Button variant="ghost" size="sm" onClick={() => navigate("/initiatives")} className="rounded-lg">
              View all <ArrowUpRight className="size-3.5" />
            </Button>
          </div>
          <ul className="mt-4 divide-y divide-black/5">
            {myInitiatives.map((mi) => (
              <li key={mi.id} className="py-3 flex items-center gap-3">
                <div className="size-9 rounded-xl bg-gradient-to-br from-indigo-500/15 to-fuchsia-500/15 grid place-items-center shrink-0">
                  <Rocket className="size-4 text-indigo-600" />
                </div>
                <div className="flex-1 min-w-0">
                  <div className="text-sm font-medium truncate">{mi.name}</div>
                  <div className="text-[11px] text-muted-foreground truncate">
                    {mi.phase} · {mi.status} · {mi.impactedRoles} impacted roles
                  </div>
                </div>
                <span
                  className={cn(
                    "text-[10px] font-semibold px-2 py-0.5 rounded-full hidden sm:inline-block",
                    statusBadge[mi.status]
                  )}
                >
                  {mi.status}
                </span>
                <span className="text-sm font-semibold w-10 text-right shrink-0">{mi.progress}%</span>
              </li>
            ))}
          </ul>
        </div>

        <div className="glass rounded-2xl p-5">
          <h3 className="font-semibold">Recent Activity</h3>
          <p className="text-xs text-muted-foreground mt-0.5">
            Latest contributions captured across the team.
          </p>
          <ul className="mt-4 space-y-3">
            {recentActivity.map((item) => (
              <li key={item.id} className="rounded-xl bg-white/60 p-3">
                <div className="text-[10px] font-semibold uppercase tracking-wide text-muted-foreground">
                  {item.category}
                </div>
                <div className="text-sm font-medium leading-snug mt-1">{item.title}</div>
                <div className="text-[11px] text-muted-foreground mt-1">{item.context}</div>
              </li>
            ))}
          </ul>
        </div>
      </section>

      {/* Highlights + Upcoming Deadlines */}
      <section className="grid lg:grid-cols-3 gap-4">
        <div className="glass rounded-2xl p-5 lg:col-span-1">
          <h3 className="font-semibold">Highlights</h3>
          <p className="text-xs text-muted-foreground mt-0.5">
            Signals worth carrying into the next team or leadership discussion.
          </p>
          <div className="mt-4 grid grid-cols-3 gap-3">
            <div>
              <div className="text-2xl font-semibold tracking-tight">{attentionHighlights.onTrackInitiatives}</div>
              <div className="text-[11px] text-muted-foreground mt-0.5">On-track Initiatives</div>
            </div>
            <div>
              <div className="text-2xl font-semibold tracking-tight">{attentionHighlights.highImpactChanges}</div>
              <div className="text-[11px] text-muted-foreground mt-0.5">High-impact changes</div>
            </div>
            <div>
              <div className="text-2xl font-semibold tracking-tight">{attentionHighlights.submittedEvidence}</div>
              <div className="text-[11px] text-muted-foreground mt-0.5">Submitted evidence</div>
            </div>
          </div>
        </div>

        <div className="glass rounded-2xl p-5 lg:col-span-2">
          <h3 className="font-semibold">Upcoming Deadlines</h3>
          <p className="text-xs text-muted-foreground mt-0.5">
            Your assigned actions plus current mock leadership-report deadlines.
          </p>
          <ul className="mt-4 divide-y divide-black/5">
            {upcomingDeadlines.map((d) => (
              <li key={d.id} className="py-3 flex items-center gap-3">
                <div className="flex-1 min-w-0">
                  <div className="text-sm font-medium truncate">{d.title}</div>
                  <div className="text-[11px] text-muted-foreground truncate">{d.type}</div>
                </div>
                <span className="text-[12px] text-muted-foreground shrink-0">{d.date}</span>
              </li>
            ))}
          </ul>
        </div>
      </section>
    </div>
  );
}
