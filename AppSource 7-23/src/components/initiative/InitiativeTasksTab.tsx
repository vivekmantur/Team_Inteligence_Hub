import { useMemo, useState } from "react";
import {
  useInitiatives,
  type TaskStatus,
  type TaskPriority,
  type InitiativeTask,
} from "./InitiativeContext";
import { UserPicker } from "@/components/system/UserPicker";
import {
  useCreateTask,
  useDeleteTask,
  useInitiativeTasks,
  useUpdateTask,
  taskStatusToLabel,
  taskStatusToWire,
  type TaskPriorityWire,
} from "@/hooks/use-initiative-tasks";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import {
  Plus,
  ListChecks,
  Flag,
  CalendarDays,
  CheckCircle2,
  Circle,
  Loader,
  XCircle,
  MessageSquare,
  Trash2,
  Pencil,
  ChevronRight,
} from "lucide-react";
import { TaskDetailDialog } from "./TaskDetailDialog";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";

interface Props {
  initiativeId: string;
}

const statuses: TaskStatus[] = ["Not Started", "In Progress", "Blocked", "Done"];
const priorities: TaskPriority[] = ["High", "Medium", "Low"];

const statusTone: Record<TaskStatus, string> = {
  "Not Started": "bg-slate-500/10 text-slate-700",
  "In Progress": "bg-sky-500/10 text-sky-700",
  Blocked: "bg-rose-500/10 text-rose-700",
  Done: "bg-emerald-500/10 text-emerald-700",
};
const priorityTone: Record<TaskPriority, string> = {
  High: "bg-rose-500/10 text-rose-700",
  Medium: "bg-amber-500/10 text-amber-700",
  Low: "bg-emerald-500/10 text-emerald-700",
};

function statusIcon(s: TaskStatus) {
  switch (s) {
    case "Done":
      return CheckCircle2;
    case "In Progress":
      return Loader;
    case "Blocked":
      return XCircle;
    default:
      return Circle;
  }
}

export function InitiativeTasksTab({ initiativeId }: Props) {
  const {
    commentsByTask,
    canEditTask,
    canDeleteTask,
  } = useInitiatives();

  // Route ids are strings; the API keys on an int.
  const numericInitiativeId = /^\d+$/.test(initiativeId) ? Number(initiativeId) : null;

  const { data: taskRecords = [], isLoading, error } =
    useInitiativeTasks(numericInitiativeId);
  const createTask = useCreateTask(numericInitiativeId);
  const updateTask = useUpdateTask(numericInitiativeId);
  const deleteTaskMutation = useDeleteTask(numericInitiativeId);

  // Mapped into the shape the existing rows and detail dialog already expect.
  const tasks: InitiativeTask[] = useMemo(
    () =>
      taskRecords.map((t): InitiativeTask => ({
        id: String(t.id),
        initiativeId: String(t.initiativeId),
        title: t.title,
        assigneeName: t.assignedToDisplayName ?? undefined,
        assigneeId: t.assignedToUserId ? String(t.assignedToUserId) : undefined,
        dueDate: t.dueDate ?? undefined,
        priority: t.priority as TaskPriority,
        status: taskStatusToLabel(t.status) as TaskStatus,
        createdBy: t.createdByDisplayName ?? "",
        createdAt: t.createdAt,
      })),
    [taskRecords]
  );

  const [filter, setFilter] = useState<TaskStatus | "All">("All");
  const [showForm, setShowForm] = useState(false);

  const [title, setTitle] = useState("");
  // The assignee is a user id — Tasks.AssignedToUserId is a foreign key.
  const [assigneeUserId, setAssigneeUserId] = useState<number | null>(null);
  const [dueDate, setDueDate] = useState("");
  const [priority, setPriority] = useState<TaskPriority>("Medium");

  // Task detail dialog
  const [selectedTask, setSelectedTask] = useState<InitiativeTask | null>(null);
  const [detailOpen, setDetailOpen] = useState(false);

  // Delete confirmation
  const [pendingDelete, setPendingDelete] = useState<InitiativeTask | null>(null);

  const filtered = useMemo(
    () => (filter === "All" ? tasks : tasks.filter((t) => t.status === filter)),
    [filter, tasks]
  );

  const counts = useMemo(() => {
    const acc: Record<string, number> = { All: tasks.length };
    statuses.forEach((s) => (acc[s] = tasks.filter((t) => t.status === s).length));
    return acc;
  }, [tasks]);

  const canSubmit = title.trim().length >= 3;

  const handleCreate = () => {
    if (!canSubmit) return;

    createTask.mutate(
      {
        title: title.trim(),
        assignedToUserId: assigneeUserId,
        dueDate: dueDate || null,
        priority,
        status: "NotStarted",
      },
      {
        onSuccess: () => {
          setTitle("");
          setAssigneeUserId(null);
          setDueDate("");
          setPriority("Medium");
          setShowForm(false);
        },
      }
    );
  };

  /** Status chips and the row dropdown both send a full replace. */
  const changeStatus = (task: InitiativeTask, next: TaskStatus) => {
    updateTask.mutate({
      taskId: Number(task.id),
      title: task.title,
      assignedToUserId: task.assigneeId ? Number(task.assigneeId) : null,
      dueDate: task.dueDate ?? null,
      priority: task.priority as TaskPriorityWire,
      status: taskStatusToWire(next),
    });
  };

  const openDetail = (t: InitiativeTask) => {
    setSelectedTask(t);
    setDetailOpen(true);
  };

  // Keep the selectedTask in sync if the underlying list changes (e.g. edit)
  const liveSelected = selectedTask
    ? tasks.find((t) => t.id === selectedTask.id) || null
    : null;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-lg font-semibold tracking-tight">Tasks</h3>
          <p className="text-[12px] text-muted-foreground">
            Assignees get notified by email + in-app. Click a task to open its discussion.
          </p>
        </div>
        <Button
          className="rounded-xl bg-copilot-gradient text-white"
          onClick={() => setShowForm((v) => !v)}
        >
          <Plus className="size-4" /> New Task
        </Button>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap gap-1.5">
        {(["All", ...statuses] as (TaskStatus | "All")[]).map((s) => (
          <button
            key={s}
            onClick={() => setFilter(s)}
            className={cn(
              "px-3 h-8 rounded-lg text-[12px] font-medium transition inline-flex items-center gap-1.5",
              filter === s
                ? "bg-copilot-gradient text-white shadow-sm"
                : "bg-white/80 border border-white/70 hover:bg-white"
            )}
          >
            {s}
            <span
              className={cn(
                "text-[10px] rounded-full px-1.5 min-w-5 text-center",
                filter === s ? "bg-white/25 text-white" : "bg-muted text-muted-foreground"
              )}
            >
              {counts[s] || 0}
            </span>
          </button>
        ))}
      </div>

      {showForm && (
        <div className="glass rounded-2xl p-4 grid md:grid-cols-2 gap-3">
          <div className="md:col-span-2">
            <label className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider">
              Task
            </label>
            <Input
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="What needs to be done?…"
              className="mt-1 h-10 rounded-xl bg-white/80 border-white/70"
            />
          </div>
          <div>
            <label className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider">
              Assignee
            </label>
            <div className="mt-1">
              <UserPicker
                value={assigneeUserId}
                onChange={setAssigneeUserId}
                placeholder="Search anyone…"
              />
            </div>
          </div>
          <div>
            <label className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider">
              Due date
            </label>
            <Input
              type="date"
              value={dueDate}
              onChange={(e) => setDueDate(e.target.value)}
              className="mt-1 h-10 rounded-xl bg-white/80 border-white/70"
            />
          </div>
          <div className="md:col-span-2">
            <label className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider">
              Priority
            </label>
            <div className="mt-1 flex gap-2">
              {priorities.map((p) => (
                <button
                  key={p}
                  type="button"
                  onClick={() => setPriority(p)}
                  className={cn(
                    "flex-1 h-10 rounded-lg border text-sm font-medium inline-flex items-center justify-center gap-2 transition",
                    priority === p
                      ? "bg-copilot-gradient text-white border-transparent shadow-sm"
                      : "bg-white/80 border-white/70 hover:bg-white"
                  )}
                >
                  <Flag className="size-3.5" /> {p}
                </button>
              ))}
            </div>
          </div>
          <div className="md:col-span-2 flex justify-end gap-2">
            <Button variant="ghost" className="rounded-lg" onClick={() => setShowForm(false)}>
              Cancel
            </Button>
            <Button
              onClick={handleCreate}
              disabled={!canSubmit || createTask.isPending}
              className="rounded-lg bg-copilot-gradient text-white"
            >
              <Plus className="size-4" /> Create Task
            </Button>
          </div>
        </div>
      )}

      {/* Server rejections: title too short, inactive assignee, stale task. */}
      {(error || createTask.error || updateTask.error || deleteTaskMutation.error) && (
        <div className="rounded-xl border border-rose-300/70 bg-rose-50/80 px-3 py-2 flex items-start gap-2 text-xs text-rose-900">
          <XCircle className="size-4 shrink-0 mt-px text-rose-600" />
          <span className="break-words">
            {(error || createTask.error || updateTask.error || deleteTaskMutation.error)
              ?.message}
          </span>
        </div>
      )}

      {isLoading && <div className="glass rounded-xl h-16 animate-pulse" />}

      {/* Task list */}
      <div className="grid gap-2">
        {filtered.map((t) => {
          const Icon = statusIcon(t.status);
          const commentCount = (commentsByTask[t.id] || []).length;
          const canEdit = canEditTask(initiativeId, t);
          const canDelete = canDeleteTask(initiativeId);
          return (
            <div
              key={t.id}
              className="glass rounded-xl p-3 flex flex-col md:flex-row md:items-center gap-3 hover:shadow-md transition-shadow"
            >
              <div className="flex items-start gap-2 min-w-0 flex-1">
                <button
                  onClick={() =>
                    changeStatus(t, t.status === "Done" ? "Not Started" : "Done")
                  }
                  className={cn(
                    "mt-0.5 size-6 rounded-full grid place-items-center border transition shrink-0",
                    t.status === "Done"
                      ? "bg-emerald-500 border-emerald-500 text-white"
                      : "bg-white/80 border-white/70 text-muted-foreground hover:text-foreground"
                  )}
                  aria-label="Toggle done"
                >
                  <Icon className="size-3.5" />
                </button>
                <button
                  onClick={() => openDetail(t)}
                  className="text-left min-w-0 flex-1"
                >
                  <div
                    className={cn(
                      "text-sm font-medium leading-snug hover:text-primary transition-colors",
                      t.status === "Done" && "line-through text-muted-foreground"
                    )}
                  >
                    {t.title}
                  </div>
                  <div className="mt-0.5 flex flex-wrap items-center gap-1.5 text-[11px] text-muted-foreground">
                    {t.assigneeName && <span>Assigned to {t.assigneeName}</span>}
                    {t.dueDate && (
                      <span className="inline-flex items-center gap-1">
                        <CalendarDays className="size-3" />
                        {t.dueDate}
                      </span>
                    )}
                    {commentCount > 0 && (
                      <span className="inline-flex items-center gap-1">
                        <MessageSquare className="size-3" /> {commentCount}
                      </span>
                    )}
                    {t.fromMention && (
                      <span className="text-[10px] px-1.5 py-0.5 rounded-full bg-fuchsia-500/10 text-fuchsia-700 font-semibold">
                        From @mention
                      </span>
                    )}
                  </div>
                </button>
              </div>

              <div className="flex items-center gap-2">
                <span className={cn("text-[10px] font-semibold px-2 py-0.5 rounded-full", priorityTone[t.priority])}>
                  {t.priority}
                </span>
                <select
                  value={t.status}
                  onChange={(e) => changeStatus(t, e.target.value as TaskStatus)}
                  className={cn(
                    "h-8 rounded-lg px-2 text-[11px] font-semibold border-0 focus:outline-none",
                    statusTone[t.status]
                  )}
                >
                  {statuses.map((s) => (
                    <option key={s} value={s}>
                      {s}
                    </option>
                  ))}
                </select>
                {canEdit && (
                  <button
                    onClick={() => openDetail(t)}
                    className="size-8 grid place-items-center rounded-lg text-muted-foreground hover:text-foreground hover:bg-white/80"
                    aria-label="Edit task"
                    title="Edit"
                  >
                    <Pencil className="size-3.5" />
                  </button>
                )}
                {canDelete && (
                  <button
                    onClick={() => setPendingDelete(t)}
                    className="size-8 grid place-items-center rounded-lg text-muted-foreground hover:text-rose-600 hover:bg-rose-500/10"
                    aria-label="Delete task"
                    title="Delete"
                  >
                    <Trash2 className="size-3.5" />
                  </button>
                )}
                <button
                  onClick={() => openDetail(t)}
                  className="size-8 grid place-items-center rounded-lg text-muted-foreground hover:text-foreground hover:bg-white/80"
                  aria-label="Open task"
                  title="Open"
                >
                  <ChevronRight className="size-3.5" />
                </button>
              </div>
            </div>
          );
        })}
        {filtered.length === 0 && (
          <div className="glass rounded-2xl p-8 text-center">
            <div className="mx-auto size-12 rounded-2xl bg-copilot-gradient grid place-items-center text-white shadow-md">
              <ListChecks className="size-5" />
            </div>
            <h4 className="mt-3 font-semibold">No tasks yet</h4>
            <p className="text-[12px] text-muted-foreground mt-1">
              Create the first task — or @mention a teammate in a comment to auto-create one.
            </p>
          </div>
        )}
      </div>

      {/* Task detail */}
      <TaskDetailDialog
        open={detailOpen}
        onOpenChange={(v) => {
          setDetailOpen(v);
          if (!v) setSelectedTask(null);
        }}
        task={liveSelected}
        onRequestDelete={(t) => {
          setDetailOpen(false);
          setPendingDelete(t);
        }}
      />

      {/* Delete confirmation */}
      <Dialog open={!!pendingDelete} onOpenChange={(v) => !v && setPendingDelete(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <span className="size-8 rounded-lg bg-rose-500/10 text-rose-600 grid place-items-center">
                <Trash2 className="size-4" />
              </span>
              Delete task?
            </DialogTitle>
            <DialogDescription>
              This will permanently remove{" "}
              <span className="font-medium text-foreground">“{pendingDelete?.title}”</span> and all
              of its discussion. This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="ghost" className="rounded-lg" onClick={() => setPendingDelete(null)}>
              Cancel
            </Button>
            <Button
              className="rounded-lg bg-rose-600 text-white hover:bg-rose-700"
              onClick={() => {
                if (pendingDelete) {
                  deleteTaskMutation.mutate(Number(pendingDelete.id));
                  setPendingDelete(null);
                }
              }}
            >
              <Trash2 className="size-4" /> Delete task
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
