import { useState } from "react";
import { Award, Check, Pencil, Users, X } from "lucide-react";
import { PageHeader } from "@/components/system/PageHeader";
import { cn } from "@/lib/utils";
import { useTeamCapacity, type TeamCapacityRow } from "@/hooks/use-team-capacity";
import { useBackendUser } from "@/hooks/use-backend-user";
import { useUpdateAppRole } from "@/hooks/use-users";

/**
 * There's no skills or score data model anywhere in the backend — this is illustrative
 * content layered onto real user identities, cycled by row index so it's stable across
 * reloads rather than randomized. AppRole (the role/title line) is real, not part of this.
 */
const MOCK_EXPERTISE = [
  { skills: ["Role Hub", "Enablement"], score: 94 },
  { skills: ["Playbooks", "GTM"], score: 91 },
  { skills: ["Customer Zero", "Narrative"], score: 96 },
  { skills: ["Analytics", "AI"], score: 88 },
  { skills: ["Executive", "Marketing"], score: 90 },
];

export default function AboutTeamPage() {
  const { data: rows, isLoading } = useTeamCapacity();
  const { data: backendUser } = useBackendUser();
  const updateAppRole = useUpdateAppRole();

  const [editingUserId, setEditingUserId] = useState<number | null>(null);
  const [draftAppRole, setDraftAppRole] = useState("");

  function startEditing(row: TeamCapacityRow) {
    setEditingUserId(row.userId);
    setDraftAppRole(row.appRole);
  }

  function cancelEditing() {
    setEditingUserId(null);
    setDraftAppRole("");
  }

  async function saveAppRole(userId: number) {
    const trimmed = draftAppRole.trim();
    if (!trimmed) return;

    try {
      await updateAppRole.mutateAsync({ userId, appRole: trimmed });
      setEditingUserId(null);
    } catch {
      // Leave the field open with what was typed so the person can retry or cancel.
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Team"
        title="Ownership, Capacity & Contributions"
        description="See who owns the work, current allocations, and areas of expertise."
      />

      <div className="grid lg:grid-cols-[1.6fr_1fr] gap-4">
        <section className="glass rounded-2xl p-5">
          <h3 className="font-semibold">Team Capacity</h3>
          <p className="text-xs text-muted-foreground mt-0.5">
            Allocation totals across Initiative team assignments.
          </p>

          {isLoading ? (
            <div className="py-8 text-center text-sm text-muted-foreground">Loading team…</div>
          ) : rows.length === 0 ? (
            <div className="py-8 text-center text-sm text-muted-foreground">No active users yet.</div>
          ) : (
            <ul className="mt-4 divide-y divide-black/5">
              {rows.map((row) => {
                const overAllocated = row.totalAllocationAcrossInitiatives > 100;
                return (
                  <li key={row.userId} className="py-3">
                    <div className="flex items-center justify-between gap-3">
                      <span className="text-sm font-medium">{row.displayName}</span>
                      <span
                        className={cn(
                          "text-sm font-semibold",
                          overAllocated ? "text-rose-600" : "text-foreground",
                        )}
                      >
                        {row.totalAllocationAcrossInitiatives}%
                      </span>
                    </div>
                    <div className="mt-1.5 h-1.5 rounded-full bg-muted overflow-hidden">
                      <div
                        className="h-full bg-copilot-gradient"
                        style={{ width: `${Math.min(row.totalAllocationAcrossInitiatives, 100)}%` }}
                      />
                    </div>
                    <div className="mt-1 text-[11px] text-muted-foreground">
                      Across {row.initiativeCount} Initiative{row.initiativeCount === 1 ? "" : "s"}
                    </div>
                  </li>
                );
              })}
            </ul>
          )}
        </section>

        <section className="glass rounded-2xl p-5">
          <div className="flex items-center gap-2">
            <Users className="size-4 text-indigo-600" />
            <h3 className="font-semibold">Skills & Expertise</h3>
          </div>

          {isLoading ? (
            <div className="py-8 text-center text-sm text-muted-foreground">Loading team…</div>
          ) : (
            <ul className="mt-4 divide-y divide-black/5">
              {rows.map((row, index) => {
                const expertise = MOCK_EXPERTISE[index % MOCK_EXPERTISE.length];
                const isMe = backendUser?.id === row.userId;
                const isEditing = editingUserId === row.userId;

                return (
                  <li key={row.userId} className="py-3">
                    <div className="flex items-start justify-between gap-3">
                      <div className="min-w-0 flex-1">
                        <div className="text-sm font-medium">{row.displayName}</div>
                        {isEditing ? (
                          <div className="mt-1 flex items-center gap-1">
                            <input
                              autoFocus
                              value={draftAppRole}
                              onChange={(e) => setDraftAppRole(e.target.value)}
                              onKeyDown={(e) => {
                                if (e.key === "Enter") void saveAppRole(row.userId);
                                if (e.key === "Escape") cancelEditing();
                              }}
                              maxLength={100}
                              className="h-6 text-[11px] px-1.5 rounded border border-input bg-white flex-1 min-w-0"
                            />
                            <button
                              onClick={() => void saveAppRole(row.userId)}
                              disabled={updateAppRole.isPending}
                              className="size-5 rounded grid place-items-center text-emerald-600 hover:bg-emerald-500/10 shrink-0"
                              aria-label="Save role"
                            >
                              <Check className="size-3" />
                            </button>
                            <button
                              onClick={cancelEditing}
                              className="size-5 rounded grid place-items-center text-muted-foreground hover:bg-black/5 shrink-0"
                              aria-label="Cancel"
                            >
                              <X className="size-3" />
                            </button>
                          </div>
                        ) : (
                          <div className="flex items-center gap-1">
                            <span className="text-[11px] text-muted-foreground">{row.appRole}</span>
                            {isMe && (
                              <button
                                onClick={() => startEditing(row)}
                                className="size-5 rounded grid place-items-center text-muted-foreground hover:text-foreground hover:bg-black/5 shrink-0"
                                aria-label="Edit your role"
                              >
                                <Pencil className="size-3" />
                              </button>
                            )}
                          </div>
                        )}
                      </div>
                      <span className="inline-flex items-center gap-1 text-[11px] font-semibold shrink-0">
                        <Award className="size-3 text-amber-500" />
                        {expertise.score}
                      </span>
                    </div>
                    <div className="mt-2 flex flex-wrap gap-1.5">
                      {expertise.skills.map((skill) => (
                        <span
                          key={skill}
                          className="text-[10px] font-medium px-2 py-0.5 rounded-full bg-indigo-500/10 text-indigo-700"
                        >
                          {skill}
                        </span>
                      ))}
                    </div>
                  </li>
                );
              })}
            </ul>
          )}
        </section>
      </div>
    </div>
  );
}
