import { useMemo, useRef, useState, useEffect } from "react";
import {
  useInitiatives,
  INITIATIVE_ROLES,
  type InitiativeRole,
  type InitiativeTeamMember,
} from "./InitiativeContext";
import { avatarColorFor, initials } from "./PeoplePicker";
import { UserPicker } from "@/components/system/UserPicker";
import {
  useAddInitiativeMember,
  useInitiativeMembers,
  useRemoveInitiativeMember,
  useUpdateInitiativeMember,
} from "@/hooks/use-initiative-members";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import {
  Trash2,
  UserPlus,
  Mail,
  BellRing,
  Crown,
  Sparkles,
  ListChecks,
  Activity,
  MoreVertical,
  Pencil,
  Shield,
  UserMinus,
  X,
  Check,
  AlertTriangle,
} from "lucide-react";
import { useAddContribution } from "@/components/contribution/ContributionContext";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

const roleTone: Record<InitiativeRole, string> = {
  Owner: "bg-indigo-500/10 text-indigo-700",
  Contributor: "bg-emerald-500/10 text-emerald-700",
  "Change Manager": "bg-amber-500/10 text-amber-700",
  "Communications Lead": "bg-fuchsia-500/10 text-fuchsia-700",
  "Analytics Lead": "bg-sky-500/10 text-sky-700",
  Other: "bg-slate-500/10 text-slate-700",
};

interface Props {
  initiativeId: string;
}

export function InitiativeTeamTab({ initiativeId }: Props) {
  const {
    reassignTasks,
    tasksByInitiative,
    activityByInitiative,
    commentsByInitiative,
    canManageTeam,
  } = useInitiatives();
  const { contributions } = useAddContribution();

  // Route ids are strings; the API keys on an int.
  const numericInitiativeId = /^\d+$/.test(initiativeId) ? Number(initiativeId) : null;

  const { data: memberRecords = [], isLoading, error } =
    useInitiativeMembers(numericInitiativeId);
  const addMember = useAddInitiativeMember(numericInitiativeId);
  const updateMember = useUpdateInitiativeMember(numericInitiativeId);
  const removeMember = useRemoveInitiativeMember(numericInitiativeId);

  // The stored role is a single free-text column. Anything matching the preset list is
  // shown as that preset; anything else is surfaced through "Other".
  const team = useMemo(
    () =>
      memberRecords.map((m) => {
        const isPreset = (INITIATIVE_ROLES as readonly string[]).includes(m.role);
        return {
          id: String(m.id),
          initiativeId: String(m.initiativeId),
          name: m.userDisplayName,
          role: (isPreset ? m.role : "Other") as InitiativeRole,
          roleOther: isPreset ? undefined : m.role,
          responsibilityArea: m.responsibilityArea ?? undefined,
          allocation: m.allocation ?? undefined,
          totalAllocationAcrossInitiatives: m.totalAllocationAcrossInitiatives,
          avatarColor: avatarColorFor(m.userDisplayName),
          addedAt: m.joinedAt,
        } satisfies InitiativeTeamMember;
      }),
    [memberRecords]
  );

  const tasks = tasksByInitiative[initiativeId] || [];
  const activity = activityByInitiative[initiativeId] || [];
  const comments = commentsByInitiative[initiativeId] || [];
  const canManage = canManageTeam(initiativeId);

  const contributionsPerMember = useMemo(() => {
    const map: Record<string, number> = {};
    contributions
      .filter((c) => c.initiativeId === initiativeId)
      .forEach((c) => {
        c.contributors.forEach((p) => {
          map[p.name] = (map[p.name] || 0) + 1;
        });
        map[c.submittedBy] = (map[c.submittedBy] || 0) + 1;
      });
    return map;
  }, [contributions, initiativeId]);

  const tasksPerMember = useMemo(() => {
    const map: Record<string, { total: number; open: number }> = {};
    tasks.forEach((t) => {
      if (!t.assigneeName) return;
      const cur = map[t.assigneeName] || { total: 0, open: 0 };
      cur.total += 1;
      if (t.status !== "Done") cur.open += 1;
      map[t.assigneeName] = cur;
    });
    return map;
  }, [tasks]);

  const activityPerMember = useMemo(() => {
    const map: Record<string, number> = {};
    activity.forEach((a) => (map[a.actor] = (map[a.actor] || 0) + 1));
    comments.forEach((c) => (map[c.author] = (map[c.author] || 0) + 1));
    return map;
  }, [activity, comments]);

  // Add member form
  const [showAdd, setShowAdd] = useState(false);
  const [userId, setUserId] = useState<number | null>(null);
  const [role, setRole] = useState<InitiativeRole>("Contributor");
  const [roleOther, setRoleOther] = useState("");
  const [responsibility, setResponsibility] = useState("");
  const [allocation, setAllocation] = useState<string>("");

  const canSubmitAdd =
    userId !== null && (role !== "Other" || roleOther.trim().length > 0);

  const resetAddForm = () => {
    setUserId(null);
    setRole("Contributor");
    setRoleOther("");
    setResponsibility("");
    setAllocation("");
    setShowAdd(false);
  };

  const handleAdd = () => {
    if (!canSubmitAdd || userId === null) return;

    addMember.mutate(
      {
        userId,
        // The column holds one role. "Other" is a prompt to type one, not a value.
        role: role === "Other" ? roleOther.trim() : role,
        responsibilityArea: responsibility.trim() || undefined,
        allocation: allocation
          ? Math.min(100, Math.max(0, Number(allocation)))
          : undefined,
      },
      { onSuccess: resetAddForm }
    );
  };

  // Edit / remove state
  const [editing, setEditing] = useState<InitiativeTeamMember | null>(null);
  const [pendingRemove, setPendingRemove] = useState<InitiativeTeamMember | null>(null);

  return (
    <div className="space-y-5">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-lg font-semibold tracking-tight">Initiative Team</h3>
          <p className="text-[12px] text-muted-foreground">
            {canManage
              ? "You can add, edit, or remove team members. Assignees are notified by email and in-app."
              : "You have read-only access to the team list."}
          </p>
        </div>
        {canManage && (
          <Button
            className="rounded-xl bg-copilot-gradient text-white"
            onClick={() => setShowAdd((v) => !v)}
          >
            <UserPlus className="size-4" /> Add Member
          </Button>
        )}
      </div>

      {showAdd && canManage && (
        <div className="glass rounded-2xl p-4 grid md:grid-cols-2 gap-3">
          <Field label="Name">
            <UserPicker
              value={userId}
              onChange={setUserId}
              placeholder="Search people who have signed in…"
              excludeUserIds={memberRecords.map((m) => m.userId)}
            />
          </Field>
          <Field label="Role">
            <div className="flex flex-wrap gap-1.5">
              {INITIATIVE_ROLES.map((r) => (
                <button
                  key={r}
                  type="button"
                  onClick={() => setRole(r)}
                  className={cn(
                    "px-3 h-9 rounded-lg text-[12px] font-medium transition",
                    role === r
                      ? "bg-copilot-gradient text-white shadow-sm"
                      : "bg-white/80 border border-white/70 hover:bg-white"
                  )}
                >
                  {r}
                </button>
              ))}
            </div>
          </Field>
          {role === "Other" && (
            <Field label="Specify role">
              <Input
                value={roleOther}
                onChange={(e) => setRoleOther(e.target.value)}
                className="h-10 rounded-xl bg-white/80 border-white/70"
                placeholder="e.g. Legal Reviewer"
              />
            </Field>
          )}
          <Field label="Responsibility Area">
            <Input
              value={responsibility}
              onChange={(e) => setResponsibility(e.target.value)}
              className="h-10 rounded-xl bg-white/80 border-white/70"
              placeholder="e.g. APAC enablement"
            />
          </Field>
          <Field label="Allocation (optional)">
            <div className="relative">
              <Input
                type="number"
                min={0}
                max={100}
                value={allocation}
                onChange={(e) => setAllocation(e.target.value)}
                className="h-10 rounded-xl bg-white/80 border-white/70 pr-8"
                placeholder="e.g. 25"
              />
              <span className="absolute right-3 top-1/2 -translate-y-1/2 text-[11px] text-muted-foreground">
                %
              </span>
            </div>
          </Field>
          <div className="md:col-span-2 flex items-center justify-between gap-2">
            <div className="text-[11px] text-muted-foreground flex items-center gap-2">
              <BellRing className="size-3.5" /> Adding a member sends an email and in-app notification.
            </div>
            <div className="flex gap-2">
              <Button variant="ghost" className="rounded-lg" onClick={() => setShowAdd(false)}>
                Cancel
              </Button>
              <Button
                onClick={handleAdd}
                disabled={!canSubmitAdd}
                className="rounded-lg bg-copilot-gradient text-white"
              >
                <UserPlus className="size-4" /> Add
              </Button>
            </div>
          </div>
        </div>
      )}

      {!canManage && (
        <div className="rounded-xl bg-white/60 border border-white/70 px-3 py-2 text-[12px] text-muted-foreground inline-flex items-center gap-2">
          <Shield className="size-3.5" /> Only the Initiative Owner and administrators can edit or remove team members.
        </div>
      )}

      <div className="grid gap-3">
        {team.map((m) => (
          <MemberRow
            key={m.id}
            member={m}
            canManage={canManage}
            contributionCount={contributionsPerMember[m.name] || 0}
            openTasks={tasksPerMember[m.name]?.open || 0}
            totalTasks={tasksPerMember[m.name]?.total || 0}
            activityCount={activityPerMember[m.name] || 0}
            onEdit={() => setEditing(m)}
            onRequestRemove={() => setPendingRemove(m)}
          />
        ))}
        {/* Server rejections: already on the team, deactivated user, blank "Other" role. */}
        {(addMember.error || updateMember.error || removeMember.error || error) && (
          <div className="rounded-xl border border-rose-300/70 bg-rose-50/80 px-3 py-2 flex items-start gap-2 text-xs text-rose-900">
            <AlertTriangle className="size-4 shrink-0 mt-px text-rose-600" />
            <span className="break-words">
              {(addMember.error || updateMember.error || removeMember.error || error)
                ?.message}
            </span>
          </div>
        )}

        {isLoading && (
          <div className="glass rounded-2xl h-20 animate-pulse" />
        )}

        {!isLoading && team.length === 0 && (
          <div className="glass rounded-2xl p-8 text-center">
            <div className="mx-auto size-12 rounded-2xl bg-copilot-gradient grid place-items-center text-white shadow-md">
              <UserPlus className="size-5" />
            </div>
            <h4 className="mt-3 font-semibold">No team members yet</h4>
            <p className="text-[12px] text-muted-foreground mt-1">
              Add members to start collaborating on this Initiative.
            </p>
          </div>
        )}
      </div>

      {/* Edit member dialog */}
      <EditMemberDialog
        member={editing}
        onClose={() => setEditing(null)}
        onSave={(patch) => {
          if (!editing) return;

          const merged = { ...editing, ...patch };

          updateMember.mutate({
            memberId: Number(editing.id),
            role:
              merged.role === "Other"
                ? (merged.roleOther ?? "").trim()
                : merged.role,
            responsibilityArea: merged.responsibilityArea || undefined,
            allocation: merged.allocation ?? undefined,
          });

          setEditing(null);
        }}
      />

      {/* Remove member dialog */}
      <RemoveMemberDialog
        member={pendingRemove}
        onCancel={() => setPendingRemove(null)}
        onConfirm={(reassignTo, applyReassign) => {
          if (!pendingRemove) return;
          if (applyReassign) {
            reassignTasks(initiativeId, pendingRemove.name, reassignTo || undefined);
          }
          removeMember.mutate(Number(pendingRemove.id));
          setPendingRemove(null);
        }}
        openTasksForMember={
          pendingRemove
            ? tasks.filter(
                (t) => t.assigneeName === pendingRemove.name && t.status !== "Done"
              ).length
            : 0
        }
        team={team}
      />
    </div>
  );
}

/* ---------------- MemberRow with More menu ---------------- */

function MemberRow({
  member,
  canManage,
  contributionCount,
  openTasks,
  totalTasks,
  activityCount,
  onEdit,
  onRequestRemove,
}: {
  member: InitiativeTeamMember;
  canManage: boolean;
  contributionCount: number;
  openTasks: number;
  totalTasks: number;
  activityCount: number;
  onEdit: () => void;
  onRequestRemove: () => void;
}) {
  const isOwner = member.role === "Owner";
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const onDown = (e: MouseEvent) => {
      if (!menuRef.current) return;
      if (!menuRef.current.contains(e.target as Node)) setMenuOpen(false);
    };
    if (menuOpen) document.addEventListener("mousedown", onDown);
    return () => document.removeEventListener("mousedown", onDown);
  }, [menuOpen]);

  return (
    <div className="glass rounded-2xl p-4 flex flex-col md:flex-row md:items-center gap-3">
      <div className="flex items-center gap-3 min-w-0 md:w-64">
        <div
          className={cn(
            "size-11 rounded-full text-white text-[13px] font-semibold grid place-items-center bg-gradient-to-br shadow-sm shrink-0",
            member.avatarColor
          )}
        >
          {initials(member.name)}
        </div>
        <div className="min-w-0">
          <div className="font-semibold text-sm truncate flex items-center gap-1.5">
            {member.name}
            {isOwner && <Crown className="size-3.5 text-amber-500" />}
          </div>
          <div className="text-[11px] text-muted-foreground truncate flex items-center gap-1">
            <Mail className="size-3" />
            {member.name.toLowerCase().replace(/\s+/g, ".")}@contoso.com
          </div>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-2 md:gap-3 flex-1">
        <span className={cn("text-[10px] font-semibold px-2 py-0.5 rounded-full", roleTone[member.role])}>
          {member.role === "Other" && member.roleOther ? member.roleOther : member.role}
        </span>
        {member.responsibilityArea && (
          <span className="text-[11px] text-muted-foreground truncate max-w-[220px]">
            {member.responsibilityArea}
          </span>
        )}
        {typeof member.allocation === "number" && (
          <span className="text-[11px] font-medium bg-white/70 rounded-full px-2 py-0.5 border border-white/60">
            {member.allocation}% allocation
          </span>
        )}
      </div>

      <div className="grid grid-cols-3 gap-2 md:w-64">
        <Stat icon={Sparkles} label="Contribs" value={contributionCount} />
        <Stat icon={ListChecks} label="Tasks" value={`${openTasks}/${totalTasks}`} />
        <Stat icon={Activity} label="Activity" value={activityCount} />
      </div>

      {typeof member.totalAllocationAcrossInitiatives === "number" && (
        <div
          className="hidden md:flex flex-col items-center justify-center rounded-lg bg-white/70 border border-white/60 px-3 py-1.5 text-center shrink-0"
          title="Current total allocation"
        >
          <div className="text-[9px] text-muted-foreground uppercase tracking-wide">Total</div>
          <div className="text-sm font-semibold">
            {member.totalAllocationAcrossInitiatives}%
          </div>
        </div>
      )}

      {canManage && (
        <div className="relative self-start md:self-center" ref={menuRef}>
          <button
            onClick={() => setMenuOpen((v) => !v)}
            className="rounded-lg size-8 grid place-items-center text-muted-foreground hover:text-foreground hover:bg-white/80 transition"
            aria-label="More actions"
          >
            <MoreVertical className="size-4" />
          </button>
          {menuOpen && (
            <div className="absolute right-0 mt-1 w-52 rounded-xl bg-white border border-black/5 shadow-xl overflow-hidden z-30">
              <button
                onClick={() => {
                  setMenuOpen(false);
                  onEdit();
                }}
                className="w-full text-left px-3 py-2 text-[13px] hover:bg-muted inline-flex items-center gap-2"
              >
                <Pencil className="size-3.5" />
                Edit Team Member
              </button>
              {isOwner ? (
                <div className="px-3 py-2 text-[11px] text-muted-foreground border-t border-black/5 flex items-start gap-1.5">
                  <Shield className="size-3.5 mt-0.5" />
                  Owner cannot be removed. Reassign ownership first.
                </div>
              ) : (
                <button
                  onClick={() => {
                    setMenuOpen(false);
                    onRequestRemove();
                  }}
                  className="w-full text-left px-3 py-2 text-[13px] text-rose-600 hover:bg-rose-500/10 inline-flex items-center gap-2 border-t border-black/5"
                >
                  <UserMinus className="size-3.5" />
                  Remove Team Member
                </button>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function Stat({ icon: Icon, label, value }: { icon: any; label: string; value: number | string }) {
  return (
    <div className="rounded-lg bg-white/70 border border-white/60 px-2 py-1.5 text-center">
      <div className="flex items-center justify-center gap-1 text-[10px] uppercase tracking-wider text-muted-foreground">
        <Icon className="size-3" />
        {label}
      </div>
      <div className="text-[13px] font-semibold">{value}</div>
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider">
        {label}
      </label>
      <div className="mt-1">{children}</div>
    </div>
  );
}

/* ---------------- Edit member dialog ---------------- */

function EditMemberDialog({
  member,
  onClose,
  onSave,
}: {
  member: InitiativeTeamMember | null;
  onClose: () => void;
  onSave: (patch: Partial<InitiativeTeamMember>) => void;
}) {
  const [role, setRole] = useState<InitiativeRole>("Contributor");
  const [roleOther, setRoleOther] = useState("");
  const [responsibility, setResponsibility] = useState("");
  const [allocation, setAllocation] = useState("");

  useEffect(() => {
    if (!member) return;
    setRole(member.role);
    setRoleOther(member.roleOther || "");
    setResponsibility(member.responsibilityArea || "");
    setAllocation(typeof member.allocation === "number" ? String(member.allocation) : "");
  }, [member]);

  if (!member) return null;

  const canSave = role !== "Other" || roleOther.trim().length > 0;

  return (
    <Dialog open={!!member} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <span className="size-8 rounded-lg bg-copilot-gradient grid place-items-center text-white">
              <Pencil className="size-4" />
            </span>
            Edit team member
          </DialogTitle>
          <DialogDescription>
            Update <span className="font-medium text-foreground">{member.name}</span>’s role and
            responsibility area on this Initiative.
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-3 py-1">
          <div>
            <label className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider">
              Role
            </label>
            <div className="mt-1 flex flex-wrap gap-1.5">
              {INITIATIVE_ROLES.map((r) => (
                <button
                  key={r}
                  type="button"
                  onClick={() => setRole(r)}
                  disabled={member.role === "Owner" && r !== "Owner"}
                  className={cn(
                    "px-3 h-9 rounded-lg text-[12px] font-medium transition",
                    role === r
                      ? "bg-copilot-gradient text-white shadow-sm"
                      : "bg-white/80 border border-white/70 hover:bg-white",
                    member.role === "Owner" && r !== "Owner" && "opacity-50 cursor-not-allowed"
                  )}
                >
                  {r}
                </button>
              ))}
            </div>
            {member.role === "Owner" && (
              <div className="mt-1 text-[11px] text-muted-foreground inline-flex items-center gap-1">
                <Shield className="size-3" /> Owner role can’t be changed here.
              </div>
            )}
          </div>

          {role === "Other" && (
            <div>
              <label className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider">
                Specify role
              </label>
              <Input
                value={roleOther}
                onChange={(e) => setRoleOther(e.target.value)}
                className="mt-1 h-10 rounded-xl bg-white/80 border-white/70"
                placeholder="e.g. Legal Reviewer"
              />
            </div>
          )}

          <div>
            <label className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider">
              Responsibility Area
            </label>
            <Input
              value={responsibility}
              onChange={(e) => setResponsibility(e.target.value)}
              className="mt-1 h-10 rounded-xl bg-white/80 border-white/70"
              placeholder="e.g. APAC enablement"
            />
          </div>

          <div>
            <label className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider">
              Allocation (optional)
            </label>
            <div className="relative mt-1">
              <Input
                type="number"
                min={0}
                max={100}
                value={allocation}
                onChange={(e) => setAllocation(e.target.value)}
                className="h-10 rounded-xl bg-white/80 border-white/70 pr-8"
                placeholder="e.g. 25"
              />
              <span className="absolute right-3 top-1/2 -translate-y-1/2 text-[11px] text-muted-foreground">
                %
              </span>
            </div>
          </div>
        </div>

        <DialogFooter>
          <Button variant="ghost" className="rounded-lg" onClick={onClose}>
            <X className="size-4" /> Cancel
          </Button>
          <Button
            className="rounded-lg bg-copilot-gradient text-white"
            disabled={!canSave}
            onClick={() =>
              onSave({
                role,
                roleOther: role === "Other" ? roleOther.trim() : undefined,
                responsibilityArea: responsibility.trim() || undefined,
                allocation: allocation === "" ? undefined : Math.min(100, Math.max(0, Number(allocation))),
              })
            }
          >
            <Check className="size-4" /> Save changes
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

/* ---------------- Remove member dialog with reassignment ---------------- */

function RemoveMemberDialog({
  member,
  onCancel,
  onConfirm,
  openTasksForMember,
  team,
}: {
  member: InitiativeTeamMember | null;
  onCancel: () => void;
  onConfirm: (reassignTo: string | "", applyReassign: boolean) => void;
  openTasksForMember: number;
  team: InitiativeTeamMember[];
}) {
  const [choice, setChoice] = useState<"reassign" | "unassign">("reassign");
  const [reassignTo, setReassignTo] = useState<string>("");

  useEffect(() => {
    if (!member) return;
    // Default reassign target: current Owner (if not the member being removed)
    const owner = team.find((m) => m.role === "Owner" && m.id !== member.id);
    setReassignTo(owner ? owner.name : "");
    setChoice("reassign");
  }, [member, team]);

  if (!member) return null;
  const hasOpen = openTasksForMember > 0;

  const eligibleTargets = team.filter((m) => m.id !== member.id);

  return (
    <Dialog open={!!member} onOpenChange={(v) => !v && onCancel()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <span className="size-8 rounded-lg bg-rose-500/10 text-rose-600 grid place-items-center">
              <UserMinus className="size-4" />
            </span>
            Remove team member?
          </DialogTitle>
          <DialogDescription>
            <span className="font-medium text-foreground">{member.name}</span> will lose access to this
            Initiative. This action cannot be undone.
          </DialogDescription>
        </DialogHeader>

        {hasOpen ? (
          <div className="rounded-xl bg-amber-500/10 border border-amber-500/30 p-3 text-[12px] text-amber-800 flex items-start gap-2">
            <AlertTriangle className="size-4 mt-0.5 shrink-0" />
            <div>
              <div className="font-semibold">
                {member.name} has {openTasksForMember} open task{openTasksForMember === 1 ? "" : "s"}.
              </div>
              <div className="mt-0.5">
                Choose whether to reassign them to another teammate or leave them unassigned before removing
                the member.
              </div>
            </div>
          </div>
        ) : (
          <div className="rounded-xl bg-white/60 border border-white/70 p-3 text-[12px] text-muted-foreground">
            {member.name} has no open tasks assigned.
          </div>
        )}

        {hasOpen && (
          <div className="space-y-2">
            <button
              type="button"
              onClick={() => setChoice("reassign")}
              className={cn(
                "w-full rounded-xl border p-3 text-left transition",
                choice === "reassign"
                  ? "bg-copilot-gradient text-white border-transparent shadow-sm"
                  : "bg-white/70 border-white/70 hover:bg-white"
              )}
            >
              <div className="text-[13px] font-semibold">Reassign open tasks</div>
              <div className={cn("text-[11px] mt-0.5", choice === "reassign" ? "text-white/85" : "text-muted-foreground")}>
                Move open tasks to another teammate on this Initiative.
              </div>
              {choice === "reassign" && (
                <div className="mt-2" onClick={(e) => e.stopPropagation()}>
                  <select
                    value={reassignTo}
                    onChange={(e) => setReassignTo(e.target.value)}
                    className="h-9 rounded-lg border border-white/70 bg-white text-foreground px-2 text-[12px] w-full"
                  >
                    <option value="">Select a teammate…</option>
                    {eligibleTargets.map((m) => (
                      <option key={m.id} value={m.name}>
                        {m.name} — {m.role === "Other" && m.roleOther ? m.roleOther : m.role}
                      </option>
                    ))}
                  </select>
                </div>
              )}
            </button>

            <button
              type="button"
              onClick={() => setChoice("unassign")}
              className={cn(
                "w-full rounded-xl border p-3 text-left transition",
                choice === "unassign"
                  ? "bg-copilot-gradient text-white border-transparent shadow-sm"
                  : "bg-white/70 border-white/70 hover:bg-white"
              )}
            >
              <div className="text-[13px] font-semibold">Leave tasks unassigned</div>
              <div
                className={cn(
                  "text-[11px] mt-0.5",
                  choice === "unassign" ? "text-white/85" : "text-muted-foreground"
                )}
              >
                Open tasks stay on this Initiative without an assignee.
              </div>
            </button>
          </div>
        )}

        <DialogFooter>
          <Button variant="ghost" className="rounded-lg" onClick={onCancel}>
            Cancel
          </Button>
          <Button
            className="rounded-lg bg-rose-600 text-white hover:bg-rose-700"
            disabled={hasOpen && choice === "reassign" && !reassignTo}
            onClick={() =>
              onConfirm(
                hasOpen && choice === "reassign" ? reassignTo : "",
                hasOpen // apply reassignment/unassign only when there are open tasks
              )
            }
          >
            <UserMinus className="size-4" /> Remove member
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
