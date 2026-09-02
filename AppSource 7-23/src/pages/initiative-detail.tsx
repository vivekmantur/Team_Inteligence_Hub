import { useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  ArrowLeft,
  Rocket,
  Users,
  ListChecks,
  Activity,
  Sparkles,
  BarChart3,
  Calendar,
  Target,
  ShieldCheck,
  Plus,
} from "lucide-react";
import { PageHeader } from "@/components/system/PageHeader";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { deliverables } from "@/data/mock";
import { useInitiatives } from "@/components/initiative/InitiativeContext";
import { useInitiativeQuery, statusToLabel, focusAreaToLabel } from "@/hooks/use-initiatives-api";
import { formatDistanceToNow } from "date-fns";
import { useAddContribution } from "@/components/contribution/ContributionContext";
import { InitiativeTeamTab } from "@/components/initiative/InitiativeTeamTab";
import { InitiativeTasksTab } from "@/components/initiative/InitiativeTasksTab";
import { InitiativeActivityTab } from "@/components/initiative/InitiativeActivityTab";
import { initials, avatarColorFor } from "@/components/initiative/PeoplePicker";

type Tab = "overview" | "team" | "tasks" | "activity";

export default function InitiativeDetailPage() {
  const { id = "" } = useParams();
  const navigate = useNavigate();
  const {
    teamByInitiative,
    tasksByInitiative,
    activityByInitiative,
  } = useInitiatives();
  const { contributions, openAddContribution } = useAddContribution();

  const [tab, setTab] = useState<Tab>("overview");

  // Route ids are strings; the API keys on an int. Anything else is not ours to fetch.
  const numericId = /^\d+$/.test(id) ? Number(id) : null;
  const { data: record, isLoading, error } = useInitiativeQuery(numericId);

  const initiative = useMemo(() => {
    if (!record) return undefined;
    return {
      id: String(record.id),
      name: record.name,
      description: record.description,
      workstream: focusAreaToLabel(record.businessArea),
      status: statusToLabel(record.status),
      owner: record.ownerDisplayName ?? "Unassigned",
      // No column for progress yet, so it reads as zero rather than being invented.
      progress: 0,
      updated: formatDistanceToNow(new Date(record.updatedAt), { addSuffix: true }),
    };
  }, [record]);

  const team = teamByInitiative[id] || [];
  const tasks = tasksByInitiative[id] || [];
  const activity = activityByInitiative[id] || [];
  const initiativeContribs = contributions.filter((c) => c.initiativeId === id);
  const items = deliverables.filter((d) => d.initiativeId === id);

  if (isLoading) {
    return (
      <div className="space-y-4">
        <div className="glass rounded-2xl h-24 animate-pulse" />
        <div className="glass rounded-2xl h-64 animate-pulse" />
      </div>
    );
  }

  if (!initiative) {
    return (
      <div className="space-y-4">
        <PageHeader
          title="Initiative not found"
          description={
            error
              ? `Could not load this Initiative: ${error.message}`
              : "This Initiative may have been removed or the link is stale."
          }
        />
        <Button variant="outline" className="rounded-xl" onClick={() => navigate("/initiatives")}>
          <ArrowLeft className="size-4" /> Back to Initiatives
        </Button>
      </div>
    );
  }

  const tabs: { key: Tab; label: string; icon: any; count?: number }[] = [
    { key: "overview", label: "Overview", icon: Rocket },
    { key: "team", label: "Team", icon: Users, count: team.length },
    { key: "tasks", label: "Tasks", icon: ListChecks, count: tasks.length },
    { key: "activity", label: "Activity", icon: Activity, count: activity.length },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Initiative"
        title={initiative.name}
        description={initiative.description}
        actions={
          <>
            <Button variant="ghost" className="rounded-xl" onClick={() => navigate("/initiatives")}>
              <ArrowLeft className="size-4" /> Back
            </Button>
            <Button
              onClick={() => openAddContribution({ initiativeId: initiative.id })}
              className="rounded-xl bg-copilot-gradient text-white"
            >
              <Sparkles className="size-4" /> Add Contribution
            </Button>
          </>
        }
      />

      {/* Summary strip */}
      <div className="grid md:grid-cols-4 gap-3">
        <SummaryTile icon={ShieldCheck} label="Status" value={initiative.status} tone={statusTone(initiative.status)} />
        <SummaryTile icon={Users} label="Team" value={`${team.length} member${team.length === 1 ? "" : "s"}`} />
        <SummaryTile
          icon={ListChecks}
          label="Tasks"
          value={`${tasks.filter((t) => t.status !== "Done").length} open / ${tasks.length}`}
        />
        <SummaryTile icon={Sparkles} label="Contributions" value={initiativeContribs.length} />
      </div>

      {/* Tab bar */}
      <div className="glass rounded-2xl p-1.5 flex flex-wrap gap-1">
        {tabs.map((t) => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            className={cn(
              "px-3 h-9 rounded-xl text-[13px] font-medium inline-flex items-center gap-2 transition",
              tab === t.key
                ? "bg-copilot-gradient text-white shadow-sm"
                : "text-muted-foreground hover:text-foreground hover:bg-white/70"
            )}
          >
            <t.icon className="size-4" />
            {t.label}
            {typeof t.count === "number" && (
              <span
                className={cn(
                  "text-[10px] rounded-full px-1.5 min-w-5 text-center",
                  tab === t.key ? "bg-white/25 text-white" : "bg-muted text-muted-foreground"
                )}
              >
                {t.count}
              </span>
            )}
          </button>
        ))}
      </div>

      {/* Tab content */}
      {tab === "overview" && (
        <div className="grid lg:grid-cols-[1fr_320px] gap-4">
          <div className="space-y-4">
            <div className="glass rounded-2xl p-5">
              <div className="flex items-center justify-between">
                <h3 className="font-semibold">Progress</h3>
                <span className="text-[12px] text-muted-foreground">Updated {initiative.updated}</span>
              </div>
              <div className="mt-3 flex items-center gap-3">
                <div className="flex-1 h-2 rounded-full bg-muted overflow-hidden">
                  <div className="h-full bg-copilot-gradient" style={{ width: `${initiative.progress}%` }} />
                </div>
                <span className="text-sm font-semibold">{initiative.progress}%</span>
              </div>
            </div>

            <div className="glass rounded-2xl p-5">
              <div className="flex items-center justify-between">
                <h3 className="font-semibold">Deliverables</h3>
                <span className="text-[12px] text-muted-foreground">{items.length} items</span>
              </div>
              <ul className="mt-3 divide-y divide-black/5">
                {items.length === 0 && (
                  <li className="py-6 text-center text-[12px] text-muted-foreground">
                    No deliverables yet. Add contributions or tasks to populate.
                  </li>
                )}
                {items.map((d) => (
                  <li key={d.id} className="py-2.5 flex items-center gap-3">
                    <div className="size-8 rounded-lg bg-gradient-to-br from-indigo-500/15 to-fuchsia-500/15 grid place-items-center">
                      <Target className="size-4 text-indigo-600" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="text-sm font-medium truncate">{d.title}</div>
                      <div className="text-[11px] text-muted-foreground">
                        {d.type} · due {d.due}
                      </div>
                    </div>
                    <span className="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-white/70 border border-white/60">
                      {d.status}
                    </span>
                  </li>
                ))}
              </ul>
            </div>
          </div>

          <aside className="space-y-3">
            <div className="glass rounded-2xl p-4">
              <div className="text-[11px] uppercase tracking-wider text-muted-foreground font-semibold">
                Owner
              </div>
              <div className="mt-2 flex items-center gap-2">
                <div
                  className={cn(
                    "size-9 rounded-full text-white text-[11px] font-semibold grid place-items-center bg-gradient-to-br",
                    avatarColorFor(initiative.owner)
                  )}
                >
                  {initials(initiative.owner)}
                </div>
                <div className="min-w-0">
                  <div className="text-sm font-medium truncate">{initiative.owner}</div>
                  <div className="text-[11px] text-muted-foreground truncate">
                    {initiative.workstream}
                  </div>
                </div>
              </div>
            </div>

            <div className="glass rounded-2xl p-4">
              <div className="flex items-center gap-2 mb-2">
                <Sparkles className="size-4 text-fuchsia-500" />
                <div className="text-sm font-semibold">Copilot summary</div>
              </div>
              <p className="text-[12px] leading-relaxed text-muted-foreground">
                {team.length} member{team.length === 1 ? "" : "s"} collaborating on this Initiative.
                {tasks.length > 0 &&
                  ` ${tasks.filter((t) => t.status !== "Done").length} open task${
                    tasks.filter((t) => t.status !== "Done").length === 1 ? "" : "s"
                  }.`}{" "}
                {initiativeContribs.length > 0 &&
                  ` ${initiativeContribs.length} contribution${initiativeContribs.length === 1 ? "" : "s"} captured.`}
              </p>
              <Button
                onClick={() => navigate("/copilot")}
                variant="outline"
                className="mt-3 w-full rounded-xl bg-white/80"
              >
                Ask Copilot about this Initiative
              </Button>
            </div>

            <div className="glass rounded-2xl p-4">
              <div className="text-[11px] uppercase tracking-wider text-muted-foreground font-semibold mb-1">
                Analytics
              </div>
              <button
                onClick={() => navigate("/analytics")}
                className="w-full inline-flex items-center justify-between text-[12px] font-medium rounded-lg bg-white/70 hover:bg-white px-3 py-2 transition"
              >
                <span className="inline-flex items-center gap-1.5">
                  <BarChart3 className="size-3.5" /> Aggregate by Initiative
                </span>
                <Calendar className="size-3.5 text-muted-foreground" />
              </button>
            </div>
          </aside>
        </div>
      )}

      {tab === "team" && <InitiativeTeamTab initiativeId={initiative.id} />}
      {tab === "tasks" && <InitiativeTasksTab initiativeId={initiative.id} />}
      {tab === "activity" && <InitiativeActivityTab initiativeId={initiative.id} />}

      {/* Floating add */}
      <div className="fixed bottom-6 right-6 z-30">
        <Button
          onClick={() => openAddContribution({ initiativeId: initiative.id })}
          className="rounded-full h-14 px-5 bg-copilot-gradient text-white shadow-2xl"
        >
          <Plus className="size-4" /> Contribute
        </Button>
      </div>
    </div>
  );
}

function SummaryTile({
  icon: Icon,
  label,
  value,
  tone,
}: {
  icon: any;
  label: string;
  value: string | number;
  tone?: string;
}) {
  return (
    <div className="glass rounded-2xl p-3 flex items-center gap-3">
      <div className="size-10 rounded-xl bg-gradient-to-br from-indigo-500/15 to-fuchsia-500/15 grid place-items-center text-indigo-600">
        <Icon className="size-5" />
      </div>
      <div className="min-w-0">
        <div className="text-[10px] uppercase tracking-wider text-muted-foreground">{label}</div>
        <div className={cn("text-sm font-semibold truncate", tone)}>{value}</div>
      </div>
    </div>
  );
}

function statusTone(s: string) {
  switch (s) {
    case "Active":
      return "text-emerald-700";
    case "On Hold":
      return "text-amber-700";
    case "Completed":
      return "text-indigo-700";
    default:
      return "text-slate-700";
  }
}
