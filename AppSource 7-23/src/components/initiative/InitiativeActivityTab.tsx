import { useMemo, useState } from "react";
import {
  useInitiatives,
} from "./InitiativeContext";
import {
  MentionInput,
  renderMentionedText,
  type MentionPerson,
} from "./MentionInput";
import { useUsers } from "@/hooks/use-users";
import { useBackendUser } from "@/hooks/use-backend-user";
import { useActivityFeed, useCreateActivity } from "@/hooks/use-activity";
import { formatDistanceToNow } from "date-fns";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { initials, avatarColorFor } from "./PeoplePicker";
import {
  MessageSquare,
  Sparkles,
  Send,
  UserPlus,
  UserMinus,
  ListChecks,
  AtSign,
  Rocket,
  ArrowRight,
} from "lucide-react";

interface Props {
  initiativeId: string;
}

export function InitiativeActivityTab({ initiativeId }: Props) {
  const { activityByInitiative } = useInitiatives();
  const activity = activityByInitiative[initiativeId] || [];

  // Route ids are strings; the API keys on an int.
  const numericInitiativeId = /^\d+$/.test(initiativeId) ? Number(initiativeId) : null;

  const { data: users = [] } = useUsers();
  const { data: currentUser } = useBackendUser();
  const { data: feed = [], isLoading, error } = useActivityFeed(numericInitiativeId);
  const createActivity = useCreateActivity(numericInitiativeId);

  const [text, setText] = useState("");
  const [mentions, setMentions] = useState<string[]>([]);
  const [mentionedIds, setMentionedIds] = useState<number[]>([]);
  const [createTasks, setCreateTasks] = useState(true);

  const mentionPeople: MentionPerson[] = useMemo(
    () => users.map((u) => ({ id: u.id, name: u.displayName })),
    [users]
  );

  const mentionNames = useMemo(
    () => mentionPeople.map((p) => p.name),
    [mentionPeople]
  );

  /**
   * Posts the update. With auto-create on, the API raises a task for each person
   * mentioned other than the author — the loop that used to live here.
   */
  const handlePost = () => {
    if (!text.trim()) return;

    createActivity.mutate(
      {
        activityMessage: text.trim(),
        autoCreateTaskEnabled: createTasks,
        mentionedUserIds: mentionedIds,
      },
      {
        onSuccess: () => {
          setText("");
          setMentions([]);
          setMentionedIds([]);
        },
      }
    );
  };

  const merged = useMemo(() => {
    const items = [
      ...feed.map((a) => ({
        kind: "comment" as const,
        id: String(a.id),
        at: a.createdAt,
        actor: a.userDisplayName,
        text: a.activityMessage,
        mentions: a.mentions.map((m) => m.displayName),
        createdTasks: a.createdTasks,
      })),
      ...activity
        .filter((a) => a.kind !== "comment")
        .map((a) => ({ kind: "activity" as const, id: a.id, at: a.at, actor: a.actor, entry: a })),
    ];
    items.sort((a, b) => (a.at < b.at ? 1 : -1));
    return items;
  }, [feed, activity]);

  return (
    <div className="grid md:grid-cols-[1fr_320px] gap-4">
      <div className="space-y-4">
        {/* Composer */}
        <div className="glass rounded-2xl p-4">
          <div className="flex items-center gap-2 mb-2">
            <div className="size-8 rounded-full bg-copilot-gradient grid place-items-center text-white text-[11px] font-semibold">
              {initials(currentUser?.displayName ?? "")}
            </div>
            <div className="text-[12px] text-muted-foreground">
              Post an update. Use <span className="font-semibold">@</span> to mention teammates.
            </div>
          </div>
          <MentionInput
            value={text}
            onChange={setText}
            onMentionsChange={setMentions}
            onMentionedIdsChange={setMentionedIds}
            people={mentionPeople}
            placeholder="Share an update, decision, or ask. Use @ to mention someone…"
            rows={3}
          />
          <div className="mt-2 flex flex-wrap items-center justify-between gap-2">
            <div className="flex items-center gap-2 flex-wrap text-[11px] text-muted-foreground">
              {mentions.length > 0 ? (
                <>
                  <AtSign className="size-3.5 text-fuchsia-500" />
                  <span>Mentioning:</span>
                  {mentions.map((m) => (
                    <span
                      key={m}
                      className="px-1.5 py-0.5 rounded-full bg-fuchsia-500/10 text-fuchsia-700 font-medium"
                    >
                      @{m}
                    </span>
                  ))}
                </>
              ) : (
                <span className="inline-flex items-center gap-1">
                  <Sparkles className="size-3.5 text-indigo-500" />
                  Mentioned teammates get email + in-app notifications.
                </span>
              )}
            </div>
            <div className="flex items-center gap-3">
              <label className="text-[11px] flex items-center gap-1.5 cursor-pointer">
                <input
                  type="checkbox"
                  checked={createTasks}
                  onChange={(e) => setCreateTasks(e.target.checked)}
                  className="accent-indigo-500"
                />
                Auto-create tasks for @mentions
              </label>
              <Button
                onClick={handlePost}
                disabled={!text.trim() || createActivity.isPending}
                className="rounded-lg bg-copilot-gradient text-white"
              >
                <Send className="size-4" /> {createActivity.isPending ? "Posting…" : "Post"}
              </Button>
            </div>
          </div>
        </div>

        {error && (
          <div className="rounded-xl border border-rose-300/70 bg-rose-50/80 px-3 py-2 text-[12px] text-rose-900 break-words">
            {error.message}
          </div>
        )}

        {createActivity.error && (
          <div className="rounded-xl border border-rose-300/70 bg-rose-50/80 px-3 py-2 text-[12px] text-rose-900 break-words">
            {createActivity.error.message}
          </div>
        )}

        {isLoading && <div className="glass rounded-2xl h-20 animate-pulse" />}

        {/* Timeline */}
        <div className="space-y-2">
          {merged.map((item) => (
            <div key={item.id} className="glass rounded-2xl p-3 flex gap-3">
              <div
                className={cn(
                  "size-9 rounded-full text-white text-[11px] font-semibold grid place-items-center bg-gradient-to-br shrink-0",
                  avatarColorFor(item.actor)
                )}
              >
                {initials(item.actor)}
              </div>
              <div className="flex-1 min-w-0">
                {item.kind === "comment" ? (
                  <>
                    <div className="text-[12px]">
                      <span className="font-semibold">{item.actor}</span>{" "}
                      <span className="text-muted-foreground">commented</span>
                    </div>
                    <div className="text-sm mt-0.5 leading-snug break-words">
                      {renderMentionedText(item.text, mentionNames).map((p, i) =>
                        typeof p === "string" ? (
                          <span key={i}>{p}</span>
                        ) : (
                          <span
                            key={i}
                            className="px-1 rounded bg-fuchsia-500/10 text-fuchsia-700 font-medium"
                          >
                            @{p.name}
                          </span>
                        )
                      )}
                    </div>
                    <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-[11px] text-muted-foreground">
                      <span>{formatDistanceToNow(new Date(item.at), { addSuffix: true })}</span>
                      {item.mentions.length > 0 && (
                        <span className="inline-flex items-center gap-1">
                          <AtSign className="size-3" />
                          Notified {item.mentions.join(", ")}
                        </span>
                      )}
                    </div>

                    {item.createdTasks.length > 0 && (
                      <div className="mt-1.5 flex flex-wrap gap-1.5">
                        {item.createdTasks.map((t) => (
                          <span
                            key={t.id}
                            className="inline-flex items-center gap-1 text-[11px] bg-indigo-500/10 text-indigo-700 rounded-full px-2 py-0.5"
                            title={t.title}
                          >
                            <ListChecks className="size-3" />
                            Task for {t.assignedToDisplayName ?? "someone"}
                          </span>
                        ))}
                      </div>
                    )}
                  </>
                ) : (
                  <ActivityLine kind={item.entry.kind} actor={item.actor} summary={item.entry.summary} />
                )}
              </div>
            </div>
          ))}
          {merged.length === 0 && (
            <div className="glass rounded-2xl p-8 text-center">
              <div className="mx-auto size-12 rounded-2xl bg-copilot-gradient grid place-items-center text-white shadow-md">
                <MessageSquare className="size-5" />
              </div>
              <h4 className="mt-3 font-semibold">No activity yet</h4>
              <p className="text-[12px] text-muted-foreground mt-1">
                Post the first update to get the conversation started.
              </p>
            </div>
          )}
        </div>
      </div>

      {/* Side helper */}
      <aside className="space-y-3">
        <div className="glass rounded-2xl p-4">
          <div className="flex items-center gap-2">
            <div className="size-9 rounded-xl bg-copilot-gradient grid place-items-center text-white shadow-sm">
              <Sparkles className="size-4" />
            </div>
            <div>
              <div className="text-sm font-semibold">Copilot summary</div>
              <div className="text-[11px] text-muted-foreground">Grounded on team activity</div>
            </div>
          </div>
          <p className="mt-3 text-[12px] leading-relaxed">
            {merged.length === 0
              ? "Once your team posts updates and completes tasks, Copilot will summarize progress here for executive briefs and QBRs."
              : `Team logged ${merged.length} activit${merged.length === 1 ? "y" : "ies"} recently. Notifications, tasks, and knowledge assets are all linked back to this Initiative.`}
          </p>
        </div>

        <div className="glass rounded-2xl p-4">
          <div className="text-[11px] uppercase tracking-wider text-muted-foreground font-semibold">
            Tips
          </div>
          <ul className="mt-2 space-y-1.5 text-[12px]">
            <li className="flex items-start gap-1.5">
              <AtSign className="size-3.5 text-fuchsia-500 mt-0.5" />
              <span>@mention teammates to loop them in — emails + in-app pings go out.</span>
            </li>
            <li className="flex items-start gap-1.5">
              <ListChecks className="size-3.5 text-indigo-500 mt-0.5" />
              <span>Toggle auto-task on to convert @mentions into assigned tasks instantly.</span>
            </li>
            <li className="flex items-start gap-1.5">
              <Rocket className="size-3.5 text-emerald-500 mt-0.5" />
              <span>Everything here powers Copilot, Analytics, and the Knowledge Repository.</span>
            </li>
          </ul>
        </div>
      </aside>
    </div>
  );
}

function ActivityLine({ kind, actor, summary }: { kind: string; actor: string; summary: string }) {
  let Icon: any = ArrowRight;
  let tone = "text-muted-foreground";
  switch (kind) {
    case "member-added":
      Icon = UserPlus;
      tone = "text-emerald-600";
      break;
    case "member-removed":
      Icon = UserMinus;
      tone = "text-rose-600";
      break;
    case "task-created":
    case "task-status":
      Icon = ListChecks;
      tone = "text-indigo-600";
      break;
    case "initiative-created":
      Icon = Rocket;
      tone = "text-fuchsia-600";
      break;
    case "contribution":
      Icon = Sparkles;
      tone = "text-fuchsia-600";
      break;
  }
  return (
    <div className="text-[12px] flex items-center gap-1.5">
      <Icon className={cn("size-3.5", tone)} />
      <span>
        <span className="font-semibold">{actor}</span> <span className="text-muted-foreground">{summary}</span>
      </span>
    </div>
  );
}
