import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type PropsWithChildren,
} from "react";
import type { Initiative } from "@/data/mock";

/* ============================================================================
 * Types
 * ============================================================================ */

export type InitiativeRole =
  | "Owner"
  | "Contributor"
  | "Change Manager"
  | "Communications Lead"
  | "Analytics Lead"
  | "Other";

export interface InitiativeTeamMember {
  id: string;
  initiativeId: string;
  name: string;
  role: InitiativeRole;
  roleOther?: string;
  responsibilityArea?: string;
  allocation?: number;
  avatarColor: string;
  addedAt: string;
}

export type TaskPriority = "High" | "Medium" | "Low";
export type TaskStatus = "Not Started" | "In Progress" | "Blocked" | "Done";

export interface TaskCommentAttachment {
  id: string;
  name: string;
  size: string;
  type: string;
}

export interface TaskComment {
  id: string;
  taskId: string;
  initiativeId: string;
  parentId?: string; // for replies
  author: string;
  text: string;
  mentions: string[];
  attachments: TaskCommentAttachment[];
  createdAt: string;
  editedAt?: string;
}

export interface InitiativeTask {
  id: string;
  initiativeId: string;
  title: string;
  description?: string;
  assigneeName?: string;
  assigneeId?: string;
  dueDate?: string;
  priority: TaskPriority;
  status: TaskStatus;
  createdBy: string;
  createdAt: string;
  fromMention?: boolean;
  sourceCommentId?: string;
}

export type ActivityKind =
  | "initiative-created"
  | "member-added"
  | "member-removed"
  | "member-updated"
  | "task-created"
  | "task-updated"
  | "task-deleted"
  | "task-status"
  | "comment"
  | "task-comment"
  | "contribution"
  | "mention";

export interface ActivityEntry {
  id: string;
  initiativeId: string;
  kind: ActivityKind;
  actor: string;
  summary: string;
  detail?: string;
  at: string;
}

export interface CommentEntry {
  id: string;
  initiativeId: string;
  author: string;
  text: string;
  mentions: string[];
  createdAt: string;
}

export interface NotificationEntry {
  id: string;
  kind: "initiative-assignment" | "task-assignment" | "mention" | "comment" | "contribution" | "task-comment";
  title: string;
  message: string;
  recipient: string;
  initiativeId?: string;
  initiativeName?: string;
  taskId?: string;
  channels: ("email" | "in-app")[];
  read: boolean;
  createdAt: string;
}

export interface CreatedInitiative extends Initiative {
  initiativeType: string;
  priority: "High" | "Medium" | "Low";
  executiveSponsor?: string;
  segment: string;
  impactedRoles: string[];
  changeImpact: "Low" | "Medium" | "High";
  startDate: string;
  targetEndDate: string;
  lifecycleStage: string;
  health: string;
  currentPhase: string;
  keyObjective?: string;
  expectedOutcome?: string;
  successMeasures?: string;
  isDraft: boolean;
  createdAt: string;
}

export interface TeamDraftMember {
  name: string;
  role: InitiativeRole;
  roleOther?: string;
  responsibilityArea?: string;
  allocation?: number;
  avatarColor: string;
}

/* ============================================================================
 * Context value
 * ============================================================================ */

interface InitiativeContextValue {
  // Current user (simulated)
  currentUser: string;
  isAdmin: boolean;

  createdInitiatives: CreatedInitiative[];
  addInitiative: (i: CreatedInitiative, team?: TeamDraftMember[]) => void;

  // Team
  teamByInitiative: Record<string, InitiativeTeamMember[]>;
  addTeamMember: (initiativeId: string, member: Omit<InitiativeTeamMember, "id" | "addedAt">) => void;
  removeTeamMember: (initiativeId: string, memberId: string) => void;
  updateTeamMember: (initiativeId: string, memberId: string, patch: Partial<InitiativeTeamMember>) => void;

  // Tasks
  tasksByInitiative: Record<string, InitiativeTask[]>;
  addTask: (task: Omit<InitiativeTask, "id" | "createdAt">) => InitiativeTask;
  updateTask: (initiativeId: string, taskId: string, patch: Partial<InitiativeTask>) => void;
  updateTaskStatus: (initiativeId: string, taskId: string, status: TaskStatus) => void;
  deleteTask: (initiativeId: string, taskId: string) => void;
  reassignTasks: (initiativeId: string, fromName: string, toName?: string) => void;

  // Task comments
  commentsByTask: Record<string, TaskComment[]>;
  addTaskComment: (
    input: Omit<TaskComment, "id" | "createdAt" | "editedAt">
  ) => TaskComment;
  updateTaskComment: (taskId: string, commentId: string, text: string, mentions: string[]) => void;
  deleteTaskComment: (taskId: string, commentId: string) => void;

  // Initiative-level comments
  commentsByInitiative: Record<string, CommentEntry[]>;
  addComment: (initiativeId: string, author: string, text: string, mentions: string[]) => void;
  activityByInitiative: Record<string, ActivityEntry[]>;

  // Notifications
  notifications: NotificationEntry[];
  unreadCount: number;
  markNotificationRead: (id: string) => void;
  markAllNotificationsRead: () => void;

  // Helpers / permissions
  getInitiative: (id: string) => (CreatedInitiative | Initiative) | undefined;
  isInitiativeOwner: (initiativeId: string) => boolean;
  canManageTeam: (initiativeId: string) => boolean;
  canEditTask: (initiativeId: string, task: InitiativeTask) => boolean;
  canDeleteTask: (initiativeId: string) => boolean;
}

const InitiativeContext = createContext<InitiativeContextValue | null>(null);

/* ============================================================================
 * Provider
 * ============================================================================ */

const CURRENT_USER = "Nihar Pulluri";
const IS_ADMIN = true; // simulate: current user is an admin for this environment

function uid(prefix: string) {
  return `${prefix}_${Math.random().toString(36).slice(2, 8)}${Date.now().toString(36).slice(-4)}`;
}
function nowIso() {
  return new Date().toISOString();
}

export function InitiativeProvider({ children }: PropsWithChildren) {
  const [createdInitiatives, setCreatedInitiatives] = useState<CreatedInitiative[]>([]);
  const [teamByInitiative, setTeamByInitiative] = useState<Record<string, InitiativeTeamMember[]>>({});
  const [tasksByInitiative, setTasksByInitiative] = useState<Record<string, InitiativeTask[]>>({});
  const [commentsByTask, setCommentsByTask] = useState<Record<string, TaskComment[]>>({});
  const [commentsByInitiative, setCommentsByInitiative] = useState<Record<string, CommentEntry[]>>({});
  const [activityByInitiative, setActivityByInitiative] = useState<Record<string, ActivityEntry[]>>({});
  const [notifications, setNotifications] = useState<NotificationEntry[]>([]);

  const pushActivity = useCallback((entry: Omit<ActivityEntry, "id" | "at">) => {
    const full: ActivityEntry = { ...entry, id: uid("act"), at: nowIso() };
    setActivityByInitiative((prev) => ({
      ...prev,
      [entry.initiativeId]: [full, ...(prev[entry.initiativeId] || [])],
    }));
  }, []);

  const pushNotification = useCallback((n: Omit<NotificationEntry, "id" | "createdAt" | "read">) => {
    const full: NotificationEntry = { ...n, id: uid("ntf"), createdAt: nowIso(), read: false };
    setNotifications((prev) => [full, ...prev]);
  }, []);

  /* -------- initiative creation -------- */

  const addInitiative = useCallback(
    (i: CreatedInitiative, team?: TeamDraftMember[]) => {
      setCreatedInitiatives((prev) => [i, ...prev]);

      const seededTeam: InitiativeTeamMember[] = [];
      const ownerMember: InitiativeTeamMember = {
        id: uid("mem"),
        initiativeId: i.id,
        name: i.owner,
        role: "Owner",
        responsibilityArea: "Overall accountability",
        allocation: 100,
        avatarColor: "from-indigo-500 to-fuchsia-500",
        addedAt: nowIso(),
      };
      seededTeam.push(ownerMember);

      if (team && team.length) {
        for (const m of team) {
          if (m.name === i.owner) continue;
          seededTeam.push({
            id: uid("mem"),
            initiativeId: i.id,
            name: m.name,
            role: m.role,
            roleOther: m.roleOther,
            responsibilityArea: m.responsibilityArea,
            allocation: m.allocation,
            avatarColor: m.avatarColor,
            addedAt: nowIso(),
          });
        }
      }
      setTeamByInitiative((prev) => ({ ...prev, [i.id]: seededTeam }));

      pushActivity({
        initiativeId: i.id,
        kind: "initiative-created",
        actor: CURRENT_USER,
        summary: `created Initiative “${i.name}”`,
        detail: i.description,
      });

      for (const m of seededTeam) {
        if (m.name === CURRENT_USER) continue;
        pushNotification({
          kind: "initiative-assignment",
          title: `You’ve been added to “${i.name}”`,
          message: `${CURRENT_USER} added you as ${m.role}${m.responsibilityArea ? ` — ${m.responsibilityArea}` : ""}.`,
          recipient: m.name,
          initiativeId: i.id,
          initiativeName: i.name,
          channels: ["email", "in-app"],
        });
      }
    },
    [pushActivity, pushNotification]
  );

  /* -------- team ops -------- */

  const addTeamMember = useCallback(
    (initiativeId: string, member: Omit<InitiativeTeamMember, "id" | "addedAt">) => {
      const full: InitiativeTeamMember = { ...member, id: uid("mem"), addedAt: nowIso() };
      setTeamByInitiative((prev) => ({
        ...prev,
        [initiativeId]: [...(prev[initiativeId] || []), full],
      }));
      const initiative = createdInitiatives.find((i) => i.id === initiativeId) || null;
      pushActivity({
        initiativeId,
        kind: "member-added",
        actor: CURRENT_USER,
        summary: `added ${member.name} as ${member.role}`,
      });
      pushNotification({
        kind: "initiative-assignment",
        title: `You’ve been added to “${initiative?.name ?? "an Initiative"}”`,
        message: `${CURRENT_USER} added you as ${member.role}${member.responsibilityArea ? ` — ${member.responsibilityArea}` : ""}.`,
        recipient: member.name,
        initiativeId,
        initiativeName: initiative?.name,
        channels: ["email", "in-app"],
      });
    },
    [createdInitiatives, pushActivity, pushNotification]
  );

  const removeTeamMember = useCallback(
    (initiativeId: string, memberId: string) => {
      let removed: InitiativeTeamMember | undefined;
      setTeamByInitiative((prev) => {
        const list = prev[initiativeId] || [];
        removed = list.find((m) => m.id === memberId);
        return { ...prev, [initiativeId]: list.filter((m) => m.id !== memberId) };
      });
      if (removed) {
        pushActivity({
          initiativeId,
          kind: "member-removed",
          actor: CURRENT_USER,
          summary: `removed ${removed.name} from the Initiative Team`,
        });
      }
    },
    [pushActivity]
  );

  const updateTeamMember = useCallback(
    (initiativeId: string, memberId: string, patch: Partial<InitiativeTeamMember>) => {
      let before: InitiativeTeamMember | undefined;
      setTeamByInitiative((prev) => {
        const list = prev[initiativeId] || [];
        before = list.find((m) => m.id === memberId);
        return {
          ...prev,
          [initiativeId]: list.map((m) => (m.id === memberId ? { ...m, ...patch } : m)),
        };
      });
      if (before) {
        const changes: string[] = [];
        if (patch.role && patch.role !== before.role) changes.push(`role → ${patch.role}`);
        if (patch.responsibilityArea !== undefined && patch.responsibilityArea !== before.responsibilityArea)
          changes.push(`area → ${patch.responsibilityArea || "(none)"}`);
        if (patch.allocation !== undefined && patch.allocation !== before.allocation)
          changes.push(`allocation → ${patch.allocation ?? 0}%`);
        if (changes.length) {
          pushActivity({
            initiativeId,
            kind: "member-updated",
            actor: CURRENT_USER,
            summary: `updated ${before.name} (${changes.join(", ")})`,
          });
        }
      }
    },
    [pushActivity]
  );

  /* -------- tasks -------- */

  const addTask = useCallback(
    (task: Omit<InitiativeTask, "id" | "createdAt">) => {
      const full: InitiativeTask = { ...task, id: uid("tsk"), createdAt: nowIso() };
      setTasksByInitiative((prev) => ({
        ...prev,
        [task.initiativeId]: [full, ...(prev[task.initiativeId] || [])],
      }));
      const initiative = createdInitiatives.find((i) => i.id === task.initiativeId);
      pushActivity({
        initiativeId: task.initiativeId,
        kind: "task-created",
        actor: task.createdBy,
        summary: `created task “${task.title}”${task.assigneeName ? ` for ${task.assigneeName}` : ""}`,
      });
      if (task.assigneeName && task.assigneeName !== task.createdBy) {
        pushNotification({
          kind: "task-assignment",
          title: `New task: ${task.title}`,
          message: `${task.createdBy} assigned you a ${task.priority.toLowerCase()}-priority task${
            task.dueDate ? ` due ${task.dueDate}` : ""
          }.`,
          recipient: task.assigneeName,
          initiativeId: task.initiativeId,
          initiativeName: initiative?.name,
          taskId: full.id,
          channels: ["email", "in-app"],
        });
      }
      return full;
    },
    [createdInitiatives, pushActivity, pushNotification]
  );

  const updateTask = useCallback(
    (initiativeId: string, taskId: string, patch: Partial<InitiativeTask>) => {
      let before: InitiativeTask | undefined;
      let after: InitiativeTask | undefined;
      setTasksByInitiative((prev) => {
        const list = prev[initiativeId] || [];
        const next = list.map((t) => {
          if (t.id === taskId) {
            before = t;
            after = { ...t, ...patch };
            return after;
          }
          return t;
        });
        return { ...prev, [initiativeId]: next };
      });
      if (before && after) {
        const changes: string[] = [];
        if (patch.title && patch.title !== before.title) changes.push("title");
        if (patch.description !== undefined && patch.description !== before.description)
          changes.push("description");
        if (patch.assigneeName !== undefined && patch.assigneeName !== before.assigneeName)
          changes.push(`assignee → ${patch.assigneeName || "(none)"}`);
        if (patch.dueDate !== undefined && patch.dueDate !== before.dueDate)
          changes.push(`due → ${patch.dueDate || "(none)"}`);
        if (patch.priority && patch.priority !== before.priority)
          changes.push(`priority → ${patch.priority}`);
        if (patch.status && patch.status !== before.status)
          changes.push(`status → ${patch.status}`);

        if (changes.length) {
          pushActivity({
            initiativeId,
            kind: "task-updated",
            actor: CURRENT_USER,
            summary: `updated task “${after.title}” (${changes.join(", ")})`,
          });
        }
        // Reassignment notification
        const initiative = createdInitiatives.find((i) => i.id === initiativeId);
        if (
          patch.assigneeName !== undefined &&
          patch.assigneeName &&
          patch.assigneeName !== before.assigneeName &&
          patch.assigneeName !== CURRENT_USER
        ) {
          pushNotification({
            kind: "task-assignment",
            title: `Reassigned task: ${after.title}`,
            message: `${CURRENT_USER} assigned this task to you${
              after.dueDate ? ` — due ${after.dueDate}` : ""
            }.`,
            recipient: patch.assigneeName,
            initiativeId,
            initiativeName: initiative?.name,
            taskId,
            channels: ["email", "in-app"],
          });
        }
      }
    },
    [createdInitiatives, pushActivity, pushNotification]
  );

  const updateTaskStatus = useCallback(
    (initiativeId: string, taskId: string, status: TaskStatus) => {
      let updated: InitiativeTask | undefined;
      setTasksByInitiative((prev) => {
        const list = prev[initiativeId] || [];
        const next = list.map((t) => {
          if (t.id === taskId) {
            updated = { ...t, status };
            return updated;
          }
          return t;
        });
        return { ...prev, [initiativeId]: next };
      });
      if (updated) {
        pushActivity({
          initiativeId,
          kind: "task-status",
          actor: CURRENT_USER,
          summary: `moved “${updated.title}” to ${status}`,
        });
      }
    },
    [pushActivity]
  );

  const deleteTask = useCallback(
    (initiativeId: string, taskId: string) => {
      let removed: InitiativeTask | undefined;
      setTasksByInitiative((prev) => {
        const list = prev[initiativeId] || [];
        removed = list.find((t) => t.id === taskId);
        return { ...prev, [initiativeId]: list.filter((t) => t.id !== taskId) };
      });
      // Also clean up its discussion
      setCommentsByTask((prev) => {
        const next = { ...prev };
        delete next[taskId];
        return next;
      });
      if (removed) {
        pushActivity({
          initiativeId,
          kind: "task-deleted",
          actor: CURRENT_USER,
          summary: `deleted task “${removed.title}”`,
        });
      }
    },
    [pushActivity]
  );

  const reassignTasks = useCallback(
    (initiativeId: string, fromName: string, toName?: string) => {
      setTasksByInitiative((prev) => {
        const list = prev[initiativeId] || [];
        return {
          ...prev,
          [initiativeId]: list.map((t) =>
            t.assigneeName === fromName && t.status !== "Done"
              ? { ...t, assigneeName: toName || undefined }
              : t
          ),
        };
      });
      const initiative = createdInitiatives.find((i) => i.id === initiativeId);
      pushActivity({
        initiativeId,
        kind: "task-updated",
        actor: CURRENT_USER,
        summary: toName
          ? `reassigned open tasks from ${fromName} to ${toName}`
          : `unassigned open tasks from ${fromName}`,
      });
      if (toName && toName !== CURRENT_USER) {
        pushNotification({
          kind: "task-assignment",
          title: `You’ve received reassigned tasks`,
          message: `${CURRENT_USER} moved open tasks from ${fromName} to you.`,
          recipient: toName,
          initiativeId,
          initiativeName: initiative?.name,
          channels: ["email", "in-app"],
        });
      }
    },
    [createdInitiatives, pushActivity, pushNotification]
  );

  /* -------- task comments -------- */

  const addTaskComment = useCallback(
    (input: Omit<TaskComment, "id" | "createdAt" | "editedAt">) => {
      const full: TaskComment = { ...input, id: uid("tcm"), createdAt: nowIso() };
      setCommentsByTask((prev) => ({
        ...prev,
        [input.taskId]: [...(prev[input.taskId] || []), full],
      }));
      const initiative = createdInitiatives.find((i) => i.id === input.initiativeId);
      // Notify mentions (in-app)
      for (const name of input.mentions) {
        if (name === input.author) continue;
        pushNotification({
          kind: "mention",
          title: `${input.author} mentioned you in a task`,
          message: input.text.slice(0, 120),
          recipient: name,
          initiativeId: input.initiativeId,
          initiativeName: initiative?.name,
          taskId: input.taskId,
          channels: ["in-app"],
        });
      }
      pushActivity({
        initiativeId: input.initiativeId,
        kind: "task-comment",
        actor: input.author,
        summary: input.parentId ? `replied on a task` : `commented on a task`,
        detail: input.text,
      });
      return full;
    },
    [createdInitiatives, pushActivity, pushNotification]
  );

  const updateTaskComment = useCallback(
    (taskId: string, commentId: string, text: string, mentions: string[]) => {
      setCommentsByTask((prev) => ({
        ...prev,
        [taskId]: (prev[taskId] || []).map((c) =>
          c.id === commentId ? { ...c, text, mentions, editedAt: nowIso() } : c
        ),
      }));
    },
    []
  );

  const deleteTaskComment = useCallback((taskId: string, commentId: string) => {
    setCommentsByTask((prev) => {
      const list = prev[taskId] || [];
      // remove target and any replies to it
      const filtered = list.filter((c) => c.id !== commentId && c.parentId !== commentId);
      return { ...prev, [taskId]: filtered };
    });
  }, []);

  /* -------- initiative-level comments -------- */

  const addComment = useCallback(
    (initiativeId: string, author: string, text: string, mentions: string[]) => {
      const entry: CommentEntry = {
        id: uid("cmt"),
        initiativeId,
        author,
        text,
        mentions,
        createdAt: nowIso(),
      };
      setCommentsByInitiative((prev) => ({
        ...prev,
        [initiativeId]: [entry, ...(prev[initiativeId] || [])],
      }));
      const initiative = createdInitiatives.find((i) => i.id === initiativeId);
      pushActivity({
        initiativeId,
        kind: "comment",
        actor: author,
        summary: `commented on the Initiative`,
        detail: text,
      });
      for (const name of mentions) {
        if (name === author) continue;
        pushNotification({
          kind: "mention",
          title: `${author} mentioned you`,
          message: text.slice(0, 120),
          recipient: name,
          initiativeId,
          initiativeName: initiative?.name,
          channels: ["email", "in-app"],
        });
      }
    },
    [createdInitiatives, pushActivity, pushNotification]
  );

  /* -------- notifications -------- */

  const markNotificationRead = useCallback((id: string) => {
    setNotifications((prev) => prev.map((n) => (n.id === id ? { ...n, read: true } : n)));
  }, []);
  const markAllNotificationsRead = useCallback(() => {
    setNotifications((prev) => prev.map((n) => ({ ...n, read: true })));
  }, []);

  /* -------- helpers / permissions -------- */

  const getInitiative = useCallback(
    (id: string) => createdInitiatives.find((c) => c.id === id),
    [createdInitiatives]
  );

  const unreadCount = useMemo(() => notifications.filter((n) => !n.read).length, [notifications]);

  const isInitiativeOwner = useCallback(
    (initiativeId: string) => {
      const team = teamByInitiative[initiativeId] || [];
      const ownerMember = team.find((m) => m.role === "Owner");
      if (ownerMember && ownerMember.name === CURRENT_USER) return true;
      // Also treat as owner when the current user matches the initiative.owner field
      const initiative = createdInitiatives.find((i) => i.id === initiativeId);
      if (initiative && initiative.owner === CURRENT_USER) return true;
      return false;
    },
    [teamByInitiative, createdInitiatives]
  );

  const canManageTeam = useCallback(
    (initiativeId: string) => IS_ADMIN || isInitiativeOwner(initiativeId),
    [isInitiativeOwner]
  );

  const canEditTask = useCallback(
    (initiativeId: string, task: InitiativeTask) => {
      if (IS_ADMIN) return true;
      if (isInitiativeOwner(initiativeId)) return true;
      if (task.assigneeName === CURRENT_USER) return true;
      if (task.createdBy === CURRENT_USER) return true;
      return false;
    },
    [isInitiativeOwner]
  );

  const canDeleteTask = useCallback(
    (initiativeId: string) => IS_ADMIN || isInitiativeOwner(initiativeId),
    [isInitiativeOwner]
  );

  const value = useMemo<InitiativeContextValue>(
    () => ({
      currentUser: CURRENT_USER,
      isAdmin: IS_ADMIN,
      createdInitiatives,
      addInitiative,
      teamByInitiative,
      addTeamMember,
      removeTeamMember,
      updateTeamMember,
      tasksByInitiative,
      addTask,
      updateTask,
      updateTaskStatus,
      deleteTask,
      reassignTasks,
      commentsByTask,
      addTaskComment,
      updateTaskComment,
      deleteTaskComment,
      commentsByInitiative,
      addComment,
      activityByInitiative,
      notifications,
      unreadCount,
      markNotificationRead,
      markAllNotificationsRead,
      getInitiative,
      isInitiativeOwner,
      canManageTeam,
      canEditTask,
      canDeleteTask,
    }),
    [
      createdInitiatives,
      addInitiative,
      teamByInitiative,
      addTeamMember,
      removeTeamMember,
      updateTeamMember,
      tasksByInitiative,
      addTask,
      updateTask,
      updateTaskStatus,
      deleteTask,
      reassignTasks,
      commentsByTask,
      addTaskComment,
      updateTaskComment,
      deleteTaskComment,
      commentsByInitiative,
      addComment,
      activityByInitiative,
      notifications,
      unreadCount,
      markNotificationRead,
      markAllNotificationsRead,
      getInitiative,
      isInitiativeOwner,
      canManageTeam,
      canEditTask,
      canDeleteTask,
    ]
  );

  return <InitiativeContext.Provider value={value}>{children}</InitiativeContext.Provider>;
}

export function useInitiatives() {
  const ctx = useContext(InitiativeContext);
  if (!ctx) throw new Error("useInitiatives must be used within InitiativeProvider");
  return ctx;
}

export const CURRENT_USER_NAME = CURRENT_USER;

export const INITIATIVE_ROLES: InitiativeRole[] = [
  "Owner",
  "Contributor",
  "Change Manager",
  "Communications Lead",
  "Analytics Lead",
  "Other",
];
