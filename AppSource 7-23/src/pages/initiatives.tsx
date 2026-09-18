import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { PageHeader } from "@/components/system/PageHeader";
import { deliverables } from "@/data/mock";
import { useInitiatives } from "@/components/initiative/InitiativeContext";
import {
  useInitiativesQuery,
  useInitiativeDeletionImpact,
  useDeleteInitiative,
  statusToLabel,
  focusAreaToLabel,
} from "@/hooks/use-initiatives-api";
import { formatDistanceToNow } from "date-fns";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Search,
  Rocket,
  Plus,
  Filter,
  Sparkles,
  Users,
  ListChecks,
  ArrowRight,
  AlertTriangle,
  Pencil,
  Trash2,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useAddContribution } from "@/components/contribution/ContributionContext";

const statuses = ["All", "Active", "On Hold", "Cancelled", "Completed"] as const;

export default function InitiativesPage() {
  const [q, setQ] = useState("");
  const [status, setStatus] = useState<(typeof statuses)[number]>("All");
  const { openAddContribution } = useAddContribution();
  const { teamByInitiative, tasksByInitiative } = useInitiatives();
  const navigate = useNavigate();
  const [pendingDelete, setPendingDelete] = useState<{ id: string; name: string } | null>(null);

  // The API is the source of truth. Records created in this session are already
  // included, because creating one invalidates this query.
  const { data: apiInitiatives = [], isLoading, error } = useInitiativesQuery();

  const allInitiatives = useMemo(
    () =>
      apiInitiatives.map((i) => ({
        id: String(i.id),
        name: i.name,
        workstream: focusAreaToLabel(i.businessArea),
        status: statusToLabel(i.status),
        // Progress, tags and deliverables have no columns yet, so they read as empty
        // rather than being invented.
        progress: 0,
        owner: i.ownerDisplayName ?? "Unassigned",
        updated: formatDistanceToNow(new Date(i.updatedAt), { addSuffix: true }),
        description: i.description,
        tags: [] as string[],
      })),
    [apiInitiatives]
  );

  const filtered = useMemo(() => {
    return allInitiatives.filter(
      (p) =>
        (status === "All" || p.status === status) &&
        (q === "" ||
          p.name.toLowerCase().includes(q.toLowerCase()) ||
          p.workstream.toLowerCase().includes(q.toLowerCase()))
    );
  }, [q, status, allInitiatives]);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Strategic execution"
        title="Initiatives"
        description="Every strategic effort driving change, transformation, and organizational readiness."
        actions={
          <>
            <Button variant="outline" className="rounded-xl bg-white/70">
              <Filter className="size-4" /> Filters
            </Button>
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

      <div className="glass rounded-2xl p-3 flex flex-col md:flex-row gap-3">
        <div className="relative flex-1">
          <Search className="size-4 text-muted-foreground absolute left-3 top-1/2 -translate-y-1/2" />
          <Input
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="Search Initiatives, workstreams, owners…"
            className="h-10 pl-9 bg-white/70 rounded-xl border-white/60"
          />
        </div>
        <div className="flex gap-1.5 flex-wrap">
          {statuses.map((s) => (
            <button
              key={s}
              onClick={() => setStatus(s)}
              className={cn(
                "px-3 h-10 rounded-xl text-[12px] font-medium transition",
                status === s ? "bg-copilot-gradient text-white shadow-sm" : "bg-white/70 hover:bg-white"
              )}
            >
              {s}
            </button>
          ))}
        </div>
      </div>

      {error ? (
        <div className="glass rounded-2xl p-8 text-center">
          <div className="mx-auto size-12 rounded-2xl bg-amber-500/10 grid place-items-center text-amber-600">
            <AlertTriangle className="size-6" />
          </div>
          <h3 className="mt-4 text-lg font-semibold">Could not load Initiatives</h3>
          <p className="mt-1 text-sm text-muted-foreground max-w-md mx-auto break-words">
            {error.message}
          </p>
        </div>
      ) : isLoading ? (
        <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-4">
          {[0, 1, 2].map((i) => (
            <div key={i} className="glass rounded-2xl p-5 h-52 animate-pulse" />
          ))}
        </div>
      ) : filtered.length === 0 ? (
        <div className="glass rounded-2xl p-12 text-center">
          <div className="mx-auto size-14 rounded-2xl bg-copilot-gradient grid place-items-center text-white shadow-md">
            <Rocket className="size-6" />
          </div>
          <h3 className="mt-4 text-lg font-semibold">No Initiatives Found</h3>
          <p className="mt-1 text-sm text-muted-foreground max-w-md mx-auto">
            Create your first Initiative to start tracking change management activities, team contributions, knowledge assets, and organizational progress.
          </p>
          <Button
            className="mt-4 rounded-xl bg-copilot-gradient text-white"
            onClick={() => navigate("/initiatives/new")}
          >
            <Plus className="size-4" /> New Initiative
          </Button>
        </div>
      ) : (
        <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-4">
          {filtered.map((p) => {
            const items = deliverables.filter((d) => d.initiativeId === p.id);
            const team = teamByInitiative[p.id] || [];
            const tasks = tasksByInitiative[p.id] || [];
            const openTasks = tasks.filter((t) => t.status !== "Done").length;
            return (
              <div
                key={p.id}
                className="glass rounded-2xl p-5 hover:shadow-xl transition-shadow flex flex-col cursor-pointer"
                onClick={() => navigate(`/initiatives/${p.id}`)}
              >
                <div className="flex items-start justify-between">
                  <div className="size-10 rounded-xl bg-gradient-to-br from-indigo-500/20 to-fuchsia-500/20 grid place-items-center">
                    <Rocket className="size-5 text-indigo-600" />
                  </div>
                  <span
                    className={cn(
                      "text-[10px] font-semibold px-2 py-0.5 rounded-full",
                      p.status === "Active" && "bg-emerald-500/10 text-emerald-700",
                      p.status === "On Hold" && "bg-amber-500/10 text-amber-700",
                      p.status === "Completed" && "bg-indigo-500/10 text-indigo-700",
                      p.status === "Cancelled" && "bg-slate-500/10 text-slate-700"
                    )}
                  >
                    {p.status}
                  </span>
                </div>
                <h3 className="mt-3 font-semibold leading-tight">{p.name}</h3>
                <p className="text-xs text-muted-foreground mt-1 line-clamp-2">{p.description}</p>

                <div className="mt-3">
                  <div className="flex items-center justify-between text-[11px] text-muted-foreground mb-1">
                    <span>Initiative progress</span>
                    <span>{p.progress}%</span>
                  </div>
                  <div className="h-1.5 rounded-full bg-muted overflow-hidden">
                    <div className="h-full bg-copilot-gradient" style={{ width: `${p.progress}%` }} />
                  </div>
                </div>

                <div className="mt-3 flex flex-wrap gap-1.5">
                  {p.tags.map((t) => (
                    <span key={t} className="text-[10px] px-2 py-0.5 rounded-full bg-white/70 border border-white/60">
                      {t}
                    </span>
                  ))}
                </div>

                <div className="mt-4 grid grid-cols-3 gap-2 text-[11px]">
                  <div className="rounded-lg bg-white/60 px-2 py-1.5">
                    <div className="text-[10px] text-muted-foreground uppercase tracking-wider inline-flex items-center gap-1">
                      <Users className="size-3" /> Team
                    </div>
                    <div className="font-semibold truncate">{Math.max(team.length, 1)}</div>
                  </div>
                  <div className="rounded-lg bg-white/60 px-2 py-1.5">
                    <div className="text-[10px] text-muted-foreground uppercase tracking-wider inline-flex items-center gap-1">
                      <ListChecks className="size-3" /> Open
                    </div>
                    <div className="font-semibold truncate">{openTasks}</div>
                  </div>
                  <div className="rounded-lg bg-white/60 px-2 py-1.5">
                    <div className="text-[10px] text-muted-foreground uppercase tracking-wider">Deliv.</div>
                    <div className="font-semibold truncate">{items.length}</div>
                  </div>
                </div>

                <div className="mt-3 text-[11px] text-muted-foreground">
                  Owner {p.owner} · updated {p.updated}
                </div>

                <div
                  className="mt-4 pt-3 border-t border-black/5 flex items-center gap-2"
                  onClick={(e) => e.stopPropagation()}
                >
                  <Button
                    onClick={() => navigate(`/initiatives/${p.id}/edit`)}
                    size="sm"
                    variant="outline"
                    className="rounded-lg bg-white/80"
                  >
                    <Pencil className="size-3.5" /> Edit
                  </Button>
                  <Button
                    onClick={() => openAddContribution({ initiativeId: p.id })}
                    size="sm"
                    variant="outline"
                    className="rounded-lg bg-white/80 flex-1"
                  >
                    <Plus className="size-3.5" /> Add Contribution
                  </Button>
                  <Button
                    onClick={() => navigate(`/initiatives/${p.id}`)}
                    size="sm"
                    className="rounded-lg bg-copilot-gradient text-white"
                  >
                    Open <ArrowRight className="size-3.5" />
                  </Button>
                  <Button
                    onClick={() => setPendingDelete({ id: p.id, name: p.name })}
                    size="sm"
                    variant="outline"
                    className="rounded-lg bg-white/80 text-rose-600 hover:bg-rose-50 hover:text-rose-700 px-2.5"
                    aria-label={`Delete ${p.name}`}
                  >
                    <Trash2 className="size-3.5" />
                  </Button>
                </div>
              </div>
            );
          })}
        </div>
      )}

      <DeleteInitiativeDialog
        initiative={pendingDelete}
        onClose={() => setPendingDelete(null)}
      />
    </div>
  );
}

/**
 * Loads real counts of everything a delete would also remove — Contributions, Tasks,
 * Activity, Team members — before letting the user confirm, rather than a generic
 * "this cannot be undone" warning with no idea what "this" actually touches.
 */
function DeleteInitiativeDialog({
  initiative,
  onClose,
}: {
  initiative: { id: string; name: string } | null;
  onClose: () => void;
}) {
  const numericId = initiative && /^\d+$/.test(initiative.id) ? Number(initiative.id) : null;
  const { data: impact, isLoading: isLoadingImpact } = useInitiativeDeletionImpact(numericId);
  const deleteInitiative = useDeleteInitiative();

  if (!initiative) return null;

  const impactRows = impact
    ? [
        { label: "Contributions", count: impact.contributionCount },
        { label: "Tasks", count: impact.taskCount },
        { label: "Activity entries", count: impact.activityCount },
        { label: "Team members", count: impact.teamMemberCount },
      ].filter((row) => row.count > 0)
    : [];

  const handleConfirm = () => {
    if (numericId === null) return;
    deleteInitiative.mutate(numericId, { onSuccess: onClose });
  };

  return (
    <Dialog open={!!initiative} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <span className="size-8 rounded-lg bg-rose-500/10 text-rose-600 grid place-items-center">
              <Trash2 className="size-4" />
            </span>
            Delete Initiative?
          </DialogTitle>
          <DialogDescription>
            <span className="font-medium text-foreground">{initiative.name}</span> will be
            permanently deleted. This action cannot be undone.
          </DialogDescription>
        </DialogHeader>

        {isLoadingImpact ? (
          <div className="rounded-xl bg-white/60 border border-white/70 p-3 text-[12px] text-muted-foreground">
            Checking what else this would remove…
          </div>
        ) : impactRows.length > 0 ? (
          <div className="rounded-xl bg-amber-500/10 border border-amber-500/30 p-3 text-[12px] text-amber-800">
            <div className="font-semibold flex items-center gap-1.5">
              <AlertTriangle className="size-3.5" /> This will also delete:
            </div>
            <ul className="mt-1.5 space-y-0.5">
              {impactRows.map((row) => (
                <li key={row.label}>
                  {row.count} {row.label}
                </li>
              ))}
            </ul>
            <div className="mt-1.5">
              Any attached documents are removed too — including their file in storage.
            </div>
          </div>
        ) : (
          <div className="rounded-xl bg-white/60 border border-white/70 p-3 text-[12px] text-muted-foreground">
            Nothing else is attached to this Initiative.
          </div>
        )}

        {deleteInitiative.error && (
          <div className="text-[12px] text-rose-700">{deleteInitiative.error.message}</div>
        )}

        <DialogFooter>
          <Button variant="ghost" className="rounded-xl" onClick={onClose}>
            Cancel
          </Button>
          <Button
            onClick={handleConfirm}
            disabled={isLoadingImpact || deleteInitiative.isPending}
            className="rounded-xl bg-rose-600 hover:bg-rose-700 text-white"
          >
            {deleteInitiative.isPending ? "Deleting…" : "Delete Initiative"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
