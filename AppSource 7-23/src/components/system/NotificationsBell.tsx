import { useEffect, useRef, useState } from "react";
import { Bell, Mail, AtSign, ListChecks, Rocket, Sparkles, X, CheckCheck } from "lucide-react";
import { useInitiatives } from "@/components/initiative/InitiativeContext";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { useNavigate } from "react-router-dom";

export function NotificationsBell() {
  const { notifications, unreadCount, markNotificationRead, markAllNotificationsRead } = useInitiatives();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();

  useEffect(() => {
    const onDown = (e: MouseEvent) => {
      if (!ref.current) return;
      if (!ref.current.contains(e.target as Node)) setOpen(false);
    };
    if (open) document.addEventListener("mousedown", onDown);
    return () => document.removeEventListener("mousedown", onDown);
  }, [open]);

  return (
    <div className="relative" ref={ref}>
      <Button
        variant="ghost"
        size="icon"
        className="rounded-xl relative"
        aria-label="Notifications"
        onClick={() => setOpen((v) => !v)}
      >
        <Bell className="size-4" />
        {unreadCount > 0 && (
          <span className="absolute -top-0.5 -right-0.5 min-w-4 h-4 text-[9px] font-semibold rounded-full bg-rose-500 text-white px-1 grid place-items-center shadow">
            {unreadCount > 9 ? "9+" : unreadCount}
          </span>
        )}
      </Button>

      {open && (
        <div className="absolute right-0 mt-2 w-[340px] max-h-[70vh] rounded-2xl bg-white border border-black/5 shadow-2xl overflow-hidden z-40 flex flex-col">
          <div className="flex items-center justify-between px-3 py-2.5 border-b border-black/5">
            <div className="flex items-center gap-2">
              <div className="size-7 rounded-lg bg-copilot-gradient grid place-items-center text-white shadow-sm">
                <Bell className="size-3.5" />
              </div>
              <div>
                <div className="text-sm font-semibold">Notifications</div>
                <div className="text-[10px] text-muted-foreground">
                  {unreadCount} unread · delivered by email + in-app
                </div>
              </div>
            </div>
            <div className="flex items-center gap-1">
              {unreadCount > 0 && (
                <button
                  onClick={() => markAllNotificationsRead()}
                  className="text-[11px] font-medium text-primary hover:underline inline-flex items-center gap-1"
                >
                  <CheckCheck className="size-3" /> Mark all
                </button>
              )}
              <button
                onClick={() => setOpen(false)}
                className="size-7 grid place-items-center rounded-lg hover:bg-muted"
                aria-label="Close"
              >
                <X className="size-3.5" />
              </button>
            </div>
          </div>

          <div className="overflow-y-auto flex-1">
            {notifications.length === 0 && (
              <div className="p-6 text-center">
                <div className="mx-auto size-10 rounded-xl bg-copilot-gradient grid place-items-center text-white">
                  <Sparkles className="size-4" />
                </div>
                <div className="mt-2 text-sm font-semibold">You’re all caught up</div>
                <div className="text-[11px] text-muted-foreground mt-1">
                  New Initiative assignments, tasks, and @mentions will appear here.
                </div>
              </div>
            )}
            {notifications.map((n) => {
              const Icon = kindIcon(n.kind);
              return (
                <button
                  key={n.id}
                  onClick={() => {
                    markNotificationRead(n.id);
                    if (n.initiativeId) {
                      navigate(`/initiatives/${n.initiativeId}`);
                      setOpen(false);
                    }
                  }}
                  className={cn(
                    "w-full text-left px-3 py-2.5 flex gap-2.5 border-b border-black/5 hover:bg-muted/60 transition",
                    !n.read && "bg-indigo-50/50"
                  )}
                >
                  <div
                    className={cn(
                      "size-8 rounded-lg grid place-items-center shrink-0 text-white shadow-sm bg-gradient-to-br",
                      kindGradient(n.kind)
                    )}
                  >
                    <Icon className="size-4" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-1.5">
                      <div className="text-[12px] font-semibold truncate">{n.title}</div>
                      {!n.read && <span className="size-1.5 rounded-full bg-rose-500" />}
                    </div>
                    <div className="text-[11px] text-muted-foreground line-clamp-2 mt-0.5">
                      {n.message}
                    </div>
                    <div className="mt-1 flex flex-wrap items-center gap-1.5 text-[10px] text-muted-foreground">
                      {n.initiativeName && (
                        <span className="inline-flex items-center gap-1">
                          <Rocket className="size-2.5" />
                          {n.initiativeName}
                        </span>
                      )}
                      <span className="inline-flex items-center gap-1">
                        <Mail className="size-2.5" /> email
                      </span>
                      <span>·</span>
                      <span>→ {n.recipient}</span>
                    </div>
                  </div>
                </button>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}

function kindIcon(k: string) {
  switch (k) {
    case "mention":
      return AtSign;
    case "task-assignment":
      return ListChecks;
    case "initiative-assignment":
      return Rocket;
    case "contribution":
      return Sparkles;
    default:
      return Bell;
  }
}

function kindGradient(k: string) {
  switch (k) {
    case "mention":
      return "from-fuchsia-500 to-purple-500";
    case "task-assignment":
      return "from-indigo-500 to-sky-500";
    case "initiative-assignment":
      return "from-emerald-500 to-teal-500";
    case "contribution":
      return "from-amber-500 to-rose-500";
    default:
      return "from-slate-500 to-slate-700";
  }
}
