import { useEffect, useMemo, useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import {
  CalendarDays,
  Flag,
  MessageSquare,
  Paperclip,
  Send,
  Trash2,
  Pencil,
  Check,
  X,
  ListChecks,
  UserCircle2,
  Shield,
  AtSign,
  Reply,
  FileText,
  Info,
} from "lucide-react";
import {
  useInitiatives,
  type InitiativeTask,
  type TaskPriority,
  type TaskStatus,
  type TaskComment,
  type TaskCommentAttachment,
} from "./InitiativeContext";
import { initials, avatarColorFor } from "./PeoplePicker";
import { UserPicker } from "@/components/system/UserPicker";
import {
  useUpdateTask,
  taskStatusToWire,
  type TaskPriorityWire,
} from "@/hooks/use-initiative-tasks";
import {
  MentionInput,
  renderMentionedText,
  type MentionPerson,
} from "./MentionInput";
import { useUsers } from "@/hooks/use-users";
import {
  downloadAttachment,
  useCreateComment,
  useDeleteComment,
  useTaskComments,
  useUpdateComment,
  useUploadAttachment,
} from "@/hooks/use-task-comments";

interface Props {
  open: boolean;
  onOpenChange: (v: boolean) => void;
  task: InitiativeTask | null;
  onDeleted?: () => void;
  onRequestDelete: (task: InitiativeTask) => void;
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

export function TaskDetailDialog({ open, onOpenChange, task, onRequestDelete }: Props) {
  const {
    canEditTask,
    canDeleteTask,
    currentUser,
    teamByInitiative,
  } = useInitiatives();

  const numericInitiativeId =
    task && /^\d+$/.test(task.initiativeId) ? Number(task.initiativeId) : null;
  const updateTaskMutation = useUpdateTask(numericInitiativeId);

  const [isEditing, setIsEditing] = useState(false);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  // The assignee is a user id — Tasks.AssignedToUserId is a foreign key.
  const [assigneeUserId, setAssigneeUserId] = useState<number | null>(null);
  const [dueDate, setDueDate] = useState("");
  const [priority, setPriority] = useState<TaskPriority>("Medium");
  const [status, setStatus] = useState<TaskStatus>("Not Started");

  // Reset local edit state whenever the task changes
  useEffect(() => {
    if (!task) return;
    setTitle(task.title);
    setDescription(task.description || "");
    setAssigneeUserId(task.assigneeId ? Number(task.assigneeId) : null);
    setDueDate(task.dueDate || "");
    setPriority(task.priority);
    setStatus(task.status);
    setIsEditing(false);
  }, [task]);

  const editable = task ? canEditTask(task.initiativeId, task) : false;
  const deletable = task ? canDeleteTask(task.initiativeId) : false;

  const initiativeTeam = task ? teamByInitiative[task.initiativeId] || [] : [];

  const numericTaskId = task && /^\d+$/.test(task.id) ? Number(task.id) : null;

  const { data: users = [] } = useUsers();
  const { data: commentRecords = [], error: commentsError } =
    useTaskComments(numericTaskId);
  const createComment = useCreateComment(numericTaskId);
  const updateComment = useUpdateComment(numericTaskId);
  const deleteComment = useDeleteComment(numericTaskId);
  const uploadAttachment = useUploadAttachment(numericTaskId);

  /**
   * Posts a comment, then uploads any files against it.
   *
   * The order is forced by the schema: an attachment row carries the comment's foreign
   * key, so the comment has to exist first.
   */
  const postComment = async (
    text: string,
    mentionedUserIds: number[],
    files: File[],
    parentCommentId: number | null
  ) => {
    const created = await createComment.mutateAsync({
      commentText: text,
      parentCommentId,
      mentionedUserIds,
    });

    for (const file of files) {
      await uploadAttachment.mutateAsync({ commentId: created.id, file });
    }
  };

  const [attachmentError, setAttachmentError] = useState<string | null>(null);

  const handleDownloadAttachment = (attachmentId: number, fileName: string) => {
    if (numericTaskId === null) return;

    setAttachmentError(null);

    void downloadAttachment(numericTaskId, {
      id: attachmentId,
      fileName,
      contentType: "application/octet-stream",
      fileSize: 0,
      createdAt: "",
    }).catch((error: unknown) => {
      setAttachmentError(
        error instanceof Error ? error.message : `Could not download ${fileName}.`
      );
    });
  };

  const mentionPeople: MentionPerson[] = useMemo(
    () => users.map((u) => ({ id: u.id, name: u.displayName })),
    [users]
  );

  const comments: TaskComment[] = useMemo(
    () =>
      commentRecords.map((c) => ({
        id: String(c.id),
        taskId: String(c.taskId),
        initiativeId: task?.initiativeId ?? "",
        parentId: c.parentCommentId ? String(c.parentCommentId) : undefined,
        author: c.userDisplayName,
        text: c.commentText,
        mentions: c.mentions.map((m) => m.displayName),
        attachments: c.attachments.map((a) => ({
          id: String(a.id),
          name: a.fileName,
          size: `${Math.max(1, Math.round(a.fileSize / 1024))} KB`,
          type: a.contentType,
        })),
        createdAt: c.createdAt,
        editedAt: c.updatedAt ?? undefined,
      })),
    [commentRecords, task?.initiativeId]
  );
  const rootComments = comments.filter((c) => !c.parentId);
  const repliesByParent = useMemo(() => {
    const map: Record<string, TaskComment[]> = {};
    comments
      .filter((c) => !!c.parentId)
      .forEach((c) => {
        (map[c.parentId!] ||= []).push(c);
      });
    return map;
  }, [comments]);

  if (!task) return null;

  const handleSave = () => {
    if (title.trim().length < 3) return;

    // Description is intentionally absent: the Tasks table has no column for it, so
    // sending it would be silently dropped. See the note in the Tasks tab.
    updateTaskMutation.mutate(
      {
        taskId: Number(task.id),
        title: title.trim(),
        assignedToUserId: assigneeUserId,
        dueDate: dueDate || null,
        priority: priority as TaskPriorityWire,
        status: taskStatusToWire(status),
      },
      { onSuccess: () => setIsEditing(false) }
    );
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl p-0 overflow-hidden max-h-[85vh] flex flex-col">
        {/* Header */}
        <div className="px-5 pt-5 pb-3 border-b border-black/5">
          <DialogHeader className="p-0">
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0 flex-1">
                {isEditing ? (
                  <Input
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    className="h-10 text-base font-semibold rounded-xl bg-white/80 border-white/70"
                  />
                ) : (
                  <DialogTitle className="text-lg leading-snug break-words">
                    {task.title}
                  </DialogTitle>
                )}
                <div className="mt-1.5 flex flex-wrap items-center gap-1.5 text-[11px] text-muted-foreground">
                  <span className={cn("px-2 py-0.5 rounded-full font-semibold", statusTone[task.status])}>
                    {task.status}
                  </span>
                  <span className={cn("px-2 py-0.5 rounded-full font-semibold", priorityTone[task.priority])}>
                    {task.priority}
                  </span>
                  {task.assigneeName && (
                    <span className="inline-flex items-center gap-1">
                      <UserCircle2 className="size-3" /> {task.assigneeName}
                    </span>
                  )}
                  {task.dueDate && (
                    <span className="inline-flex items-center gap-1">
                      <CalendarDays className="size-3" /> {task.dueDate}
                    </span>
                  )}
                  <span className="inline-flex items-center gap-1">
                    <Info className="size-3" /> Created by {task.createdBy}
                  </span>
                </div>
              </div>
              <div className="flex items-center gap-1">
                {!isEditing && editable && (
                  <Button
                    size="sm"
                    variant="outline"
                    className="rounded-lg bg-white/80"
                    onClick={() => setIsEditing(true)}
                  >
                    <Pencil className="size-3.5" /> Edit
                  </Button>
                )}
                {!isEditing && deletable && (
                  <Button
                    size="sm"
                    variant="ghost"
                    className="rounded-lg text-rose-600 hover:text-rose-700 hover:bg-rose-500/10"
                    onClick={() => onRequestDelete(task)}
                  >
                    <Trash2 className="size-3.5" /> Delete
                  </Button>
                )}
              </div>
            </div>
            {!editable && !deletable && (
              <div className="mt-2 text-[11px] text-muted-foreground inline-flex items-center gap-1">
                <Shield className="size-3" /> Read-only — you don’t have edit permissions for this task.
              </div>
            )}
          </DialogHeader>
        </div>

        {/* Body */}
        <div className="flex-1 overflow-y-auto px-5 py-4 space-y-5">
          {isEditing ? (
            <div className="grid gap-3">
              <div>
                <label className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider">
                  Description
                </label>
                <textarea
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  rows={3}
                  placeholder="Add context, acceptance criteria, links…"
                  className="mt-1 w-full rounded-xl bg-white/80 border border-white/70 px-3 py-2 text-sm resize-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-indigo-300"
                />
                <p className="mt-1 text-[11px] text-amber-700">
                  Description is not saved yet — the Tasks table has no column for it.
                </p>
              </div>
              <div className="grid md:grid-cols-2 gap-3">
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
              </div>
              <div className="grid md:grid-cols-2 gap-3">
                <div>
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
                          "flex-1 h-9 rounded-lg border text-[12px] font-medium inline-flex items-center justify-center gap-1.5 transition",
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
                <div>
                  <label className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider">
                    Status
                  </label>
                  <select
                    value={status}
                    onChange={(e) => setStatus(e.target.value as TaskStatus)}
                    className="mt-1 h-10 rounded-xl border border-white/70 bg-white/80 px-3 text-sm w-full"
                  >
                    {statuses.map((s) => (
                      <option key={s} value={s}>
                        {s}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
              <div className="flex items-center justify-end gap-2 pt-1">
                <Button
                  variant="ghost"
                  className="rounded-lg"
                  onClick={() => setIsEditing(false)}
                >
                  <X className="size-4" /> Cancel
                </Button>
                <Button
                  className="rounded-lg bg-copilot-gradient text-white"
                  onClick={handleSave}
                  disabled={updateTaskMutation.isPending || title.trim().length < 3}
                >
                  <Check className="size-4" /> Save changes
                </Button>
              </div>
            </div>
          ) : (
            <>
              {task.description ? (
                <div className="rounded-xl bg-white/60 border border-white/60 p-3 text-sm leading-relaxed whitespace-pre-wrap">
                  {task.description}
                </div>
              ) : (
                <div className="rounded-xl bg-white/40 border border-dashed border-white/70 p-3 text-[12px] text-muted-foreground">
                  No description yet.{editable && " Click Edit to add context."}
                </div>
              )}
            </>
          )}

          {/* Discussion */}
          <section>
            <div className="flex items-center justify-between mb-2">
              <div className="flex items-center gap-2">
                <MessageSquare className="size-4 text-indigo-600" />
                <h4 className="text-sm font-semibold">Discussion</h4>
                <span className="text-[11px] text-muted-foreground">
                  {comments.length} comment{comments.length === 1 ? "" : "s"}
                </span>
              </div>
              <span className="text-[10px] text-muted-foreground inline-flex items-center gap-1">
                <AtSign className="size-3" /> @mention to notify
              </span>
            </div>

            {(commentsError ||
              createComment.error ||
              updateComment.error ||
              deleteComment.error ||
              uploadAttachment.error ||
              attachmentError) && (
              <div className="mb-2 rounded-xl border border-rose-300/70 bg-rose-50/80 px-3 py-2 text-[12px] text-rose-900 break-words">
                {attachmentError ??
                  (commentsError ||
                    createComment.error ||
                    updateComment.error ||
                    deleteComment.error ||
                    uploadAttachment.error)?.message}
              </div>
            )}

            {/* Composer */}
            <TaskCommentComposer
              people={mentionPeople}
              pending={createComment.isPending || uploadAttachment.isPending}
              onSubmit={(text, mentionedUserIds, files) =>
                postComment(text, mentionedUserIds, files, null)
              }
            />

            {/* Thread */}
            <div className="mt-3 space-y-3">
              {rootComments.length === 0 && (
                <div className="rounded-xl bg-white/40 border border-dashed border-white/70 p-4 text-center text-[12px] text-muted-foreground">
                  No comments yet. Start the discussion — @mention teammates on this Initiative.
                </div>
              )}
              {rootComments.map((c) => (
                <CommentItem
                  key={c.id}
                  comment={c}
                  replies={repliesByParent[c.id] || []}
                  currentUser={currentUser}
                  people={mentionPeople}
                  onDownloadAttachment={handleDownloadAttachment}
                  onEdit={(id, text, mentionedUserIds) =>
                    updateComment.mutate({
                      commentId: Number(id),
                      commentText: text,
                      mentionedUserIds,
                    })
                  }
                  onDelete={(id) => deleteComment.mutate(Number(id))}
                  onReply={(text, mentionedUserIds, files, parentId) =>
                    postComment(text, mentionedUserIds, files, Number(parentId))
                  }
                />
              ))}
            </div>

            <div className="mt-3 text-[10px] text-muted-foreground inline-flex items-center gap-1">
              <ListChecks className="size-3" />
              {mentionPeople.length} {mentionPeople.length === 1 ? "person" : "people"} can be mentioned. Anyone who has signed in is searchable.
            </div>
          </section>
        </div>
      </DialogContent>
    </Dialog>
  );
}

/* ------------ Comment composer & item ------------ */

function TaskCommentComposer({
  onSubmit,
  parentAuthor,
  autoFocus,
  people,
  pending,
}: {
  onSubmit: (
    text: string,
    mentionedUserIds: number[],
    files: File[]
  ) => Promise<void> | void;
  parentAuthor?: string;
  autoFocus?: boolean;
  people: MentionPerson[];
  pending?: boolean;
}) {
  const { currentUser } = useInitiatives();
  const [text, setText] = useState(parentAuthor ? `@${parentAuthor} ` : "");
  const [mentions, setMentions] = useState<string[]>(parentAuthor ? [parentAuthor] : []);
  const [mentionedIds, setMentionedIds] = useState<number[]>([]);
  // Real File handles, not display stubs — these are uploaded after the comment lands.
  const [files, setFiles] = useState<File[]>([]);

  const submit = async () => {
    if (!text.trim() || pending) return;

    // Clear only once the post succeeded. Wiping the picked files up front loses them
    // on a failed upload, leaving nothing to retry with.
    try {
      await onSubmit(text.trim(), mentionedIds, files);

      setText("");
      setMentions([]);
      setMentionedIds([]);
      setFiles([]);
    } catch {
      // Reported through the mutation error banner; keep what was typed and picked.
    }
  };

  const onFilePick = (e: React.ChangeEvent<HTMLInputElement>) => {
    // Read the FileList before resetting the input. A state updater runs lazily, so
    // reading e.target.files inside one would see the list already cleared by the
    // reset below and quietly append nothing.
    const picked = Array.from(e.target.files ?? []);

    // Reset so picking the same file twice still raises a change event.
    e.target.value = "";

    if (picked.length === 0) return;

    setFiles((prev) => [...prev, ...picked]);
  };

  return (
    <div className="rounded-xl bg-white/70 border border-white/70 p-2">
      <div className="flex items-start gap-2">
        <div
          className={cn(
            "size-8 rounded-full text-white text-[11px] font-semibold grid place-items-center bg-gradient-to-br shrink-0",
            avatarColorFor(currentUser)
          )}
        >
          {initials(currentUser)}
        </div>
        <div className="flex-1 min-w-0">
          <MentionInput
            value={text}
            onChange={setText}
            onMentionsChange={setMentions}
            onMentionedIdsChange={setMentionedIds}
            people={people}
            placeholder={parentAuthor ? `Reply to ${parentAuthor}…` : "Add a comment. Try @teammate…"}
            rows={autoFocus ? 3 : 2}
          />
          {files.length > 0 && (
            <div className="mt-2 flex flex-wrap gap-1.5">
              {files.map((f, index) => (
                <span
                  key={`${f.name}-${index}`}
                  className="inline-flex items-center gap-1 text-[11px] bg-white border border-white/70 rounded-full pl-2 pr-1 py-0.5"
                >
                  <FileText className="size-3" />
                  {f.name}
                  <button
                    onClick={() =>
                      setFiles((prev) => prev.filter((_, i) => i !== index))
                    }
                    className="size-4 grid place-items-center rounded-full hover:bg-muted"
                    aria-label="Remove attachment"
                  >
                    <X className="size-3" />
                  </button>
                </span>
              ))}
            </div>
          )}
          <div className="mt-1.5 flex items-center justify-between gap-2">
            <div className="flex items-center gap-1 text-[11px] text-muted-foreground">
              <label className="inline-flex items-center gap-1 cursor-pointer hover:text-foreground">
                <Paperclip className="size-3.5" />
                <span>Attach</span>
                <input type="file" multiple className="hidden" onChange={onFilePick} />
              </label>
              {mentions.length > 0 && (
                <span className="ml-2 inline-flex items-center gap-1">
                  <AtSign className="size-3" /> {mentions.length} to notify
                </span>
              )}
            </div>
            <Button
              size="sm"
              className="rounded-lg bg-copilot-gradient text-white h-8"
              onClick={() => void submit()}
              disabled={!text.trim() || pending}
            >
              <Send className="size-3.5" /> {parentAuthor ? "Reply" : "Post"}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}

function CommentItem({
  comment,
  replies,
  currentUser,
  people,
  onDownloadAttachment,
  onEdit,
  onDelete,
  onReply,
}: {
  comment: TaskComment;
  replies: TaskComment[];
  currentUser: string;
  people: MentionPerson[];
  onDownloadAttachment: (attachmentId: number, fileName: string) => void;
  onEdit: (id: string, text: string, mentionedUserIds: number[]) => void;
  onDelete: (id: string) => void;
  onReply: (
    text: string,
    mentionedUserIds: number[],
    files: File[],
    parentId: string
  ) => Promise<void> | void;
}) {
  const [editing, setEditing] = useState(false);
  const [replying, setReplying] = useState(false);
  const [text, setText] = useState(comment.text);
  const [mentions, setMentions] = useState<string[]>(comment.mentions);
  const [mentionedIds, setMentionedIds] = useState<number[]>([]);

  const isMine = comment.author === currentUser;

  return (
    <div className="rounded-xl bg-white/70 border border-white/70 p-3">
      <div className="flex items-start gap-2">
        <div
          className={cn(
            "size-8 rounded-full text-white text-[11px] font-semibold grid place-items-center bg-gradient-to-br shrink-0",
            avatarColorFor(comment.author)
          )}
        >
          {initials(comment.author)}
        </div>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 text-[11px]">
            <span className="font-semibold text-foreground">{comment.author}</span>
            <span className="text-muted-foreground">{timeAgo(comment.createdAt)}</span>
            {comment.editedAt && <span className="text-muted-foreground">(edited)</span>}
          </div>

          {editing ? (
            <div className="mt-1.5">
              <MentionInput
                value={text}
                onChange={setText}
                onMentionsChange={setMentions}
                onMentionedIdsChange={setMentionedIds}
                people={people}
                rows={2}
              />
              <div className="mt-1.5 flex items-center gap-2 justify-end">
                <Button size="sm" variant="ghost" className="rounded-lg" onClick={() => setEditing(false)}>
                  Cancel
                </Button>
                <Button
                  size="sm"
                  className="rounded-lg bg-copilot-gradient text-white"
                  onClick={() => {
                    onEdit(comment.id, text.trim(), mentionedIds);
                    setEditing(false);
                  }}
                  disabled={!text.trim()}
                >
                  Save
                </Button>
              </div>
            </div>
          ) : (
            <div className="text-sm mt-0.5 leading-snug break-words">
              {renderMentionedText(comment.text).map((p, i) =>
                typeof p === "string" ? (
                  <span key={i}>{p}</span>
                ) : (
                  <span key={i} className="px-1 rounded bg-fuchsia-500/10 text-fuchsia-700 font-medium">
                    @{p.name}
                  </span>
                )
              )}
            </div>
          )}

          {comment.attachments.length > 0 && !editing && (
            <div className="mt-2 flex flex-wrap gap-1.5">
              {comment.attachments.map((a) => (
                <button
                  key={a.id}
                  type="button"
                  onClick={() => onDownloadAttachment(Number(a.id), a.name)}
                  title={`Download ${a.name} (${a.size})`}
                  className="inline-flex items-center gap-1 text-[11px] bg-white border border-white/70 rounded-full px-2 py-0.5 hover:bg-muted transition"
                >
                  <FileText className="size-3" /> {a.name}
                </button>
              ))}
            </div>
          )}

          {!editing && (
            <div className="mt-2 flex items-center gap-3 text-[11px] text-muted-foreground">
              <button
                className="inline-flex items-center gap-1 hover:text-foreground"
                onClick={() => setReplying((v) => !v)}
              >
                <Reply className="size-3" /> Reply
              </button>
              {isMine && (
                <>
                  <button
                    className="inline-flex items-center gap-1 hover:text-foreground"
                    onClick={() => setEditing(true)}
                  >
                    <Pencil className="size-3" /> Edit
                  </button>
                  <button
                    className="inline-flex items-center gap-1 hover:text-rose-600"
                    onClick={() => onDelete(comment.id)}
                  >
                    <Trash2 className="size-3" /> Delete
                  </button>
                </>
              )}
            </div>
          )}

          {replying && (
            <div className="mt-2">
              <TaskCommentComposer
                people={people}
                parentAuthor={comment.author}
                autoFocus
                onSubmit={async (t, m, a) => {
                  await onReply(t, m, a, comment.id);
                  setReplying(false);
                }}
              />
            </div>
          )}

          {replies.length > 0 && (
            <div className="mt-3 pl-3 border-l-2 border-black/5 space-y-2">
              {replies.map((r) => (
                <ReplyItem
                  key={r.id}
                  reply={r}
                  currentUser={currentUser}
                  people={people}
                  onEdit={onEdit}
                  onDelete={onDelete}
                />
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function ReplyItem({
  reply,
  currentUser,
  people,
  onEdit,
  onDelete,
}: {
  reply: TaskComment;
  currentUser: string;
  people: MentionPerson[];
  onEdit: (id: string, text: string, mentionedUserIds: number[]) => void;
  onDelete: (id: string) => void;
}) {
  const [editing, setEditing] = useState(false);
  const [text, setText] = useState(reply.text);
  const [mentions, setMentions] = useState<string[]>(reply.mentions);
  const [mentionedIds, setMentionedIds] = useState<number[]>([]);
  const isMine = reply.author === currentUser;

  return (
    <div className="flex items-start gap-2">
      <div
        className={cn(
          "size-7 rounded-full text-white text-[10px] font-semibold grid place-items-center bg-gradient-to-br shrink-0",
          avatarColorFor(reply.author)
        )}
      >
        {initials(reply.author)}
      </div>
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 text-[11px]">
          <span className="font-semibold">{reply.author}</span>
          <span className="text-muted-foreground">{timeAgo(reply.createdAt)}</span>
          {reply.editedAt && <span className="text-muted-foreground">(edited)</span>}
        </div>
        {editing ? (
          <div className="mt-1.5">
            <MentionInput
              value={text}
              onChange={setText}
              onMentionsChange={setMentions}
              onMentionedIdsChange={setMentionedIds}
              people={people}
              rows={2}
            />
            <div className="mt-1.5 flex items-center gap-2 justify-end">
              <Button size="sm" variant="ghost" className="rounded-lg" onClick={() => setEditing(false)}>
                Cancel
              </Button>
              <Button
                size="sm"
                className="rounded-lg bg-copilot-gradient text-white"
                onClick={() => {
                  onEdit(reply.id, text.trim(), mentionedIds);
                  setEditing(false);
                }}
                disabled={!text.trim()}
              >
                Save
              </Button>
            </div>
          </div>
        ) : (
          <div className="text-sm mt-0.5 leading-snug break-words">
            {renderMentionedText(reply.text).map((p, i) =>
              typeof p === "string" ? (
                <span key={i}>{p}</span>
              ) : (
                <span key={i} className="px-1 rounded bg-fuchsia-500/10 text-fuchsia-700 font-medium">
                  @{p.name}
                </span>
              )
            )}
          </div>
        )}
        {!editing && isMine && (
          <div className="mt-1.5 flex items-center gap-3 text-[11px] text-muted-foreground">
            <button className="inline-flex items-center gap-1 hover:text-foreground" onClick={() => setEditing(true)}>
              <Pencil className="size-3" /> Edit
            </button>
            <button
              className="inline-flex items-center gap-1 hover:text-rose-600"
              onClick={() => onDelete(reply.id)}
            >
              <Trash2 className="size-3" /> Delete
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

function timeAgo(iso: string) {
  const t = new Date(iso).getTime();
  const diff = Date.now() - t;
  if (diff < 60_000) return "just now";
  if (diff < 3_600_000) return `${Math.floor(diff / 60_000)}m ago`;
  if (diff < 86_400_000) return `${Math.floor(diff / 3_600_000)}h ago`;
  return new Date(iso).toLocaleDateString();
}
