import { NavLink } from "react-router-dom";
import {
  Home,
  BarChart3,
  Rocket,
  Users,
  UserCog,
  MessageSquareQuote,
  PenSquare,
  Library,
  Sparkles,
} from "lucide-react";
import { cn } from "@/lib/utils";

const nav = [
  { to: "/", label: "Home", icon: Home, end: true },
  { to: "/analytics", label: "Insights", icon: BarChart3 },
  { to: "/initiatives", label: "Initiatives", icon: Rocket },
  { to: "/team", label: "Team Contributions", icon: Users },
  { to: "/stories", label: "Stories & Evidence", icon: MessageSquareQuote },
  { to: "/content-studio", label: "Content Studio", icon: PenSquare },
  { to: "/knowledge", label: "Knowledge Repository", icon: Library },
  { to: "/copilot", label: "AI Copilot", icon: Sparkles },
  { to: "/aboutteam", label: "Team", icon: UserCog },
];

export function AppSidebar() {
  return (
    <aside className="hidden lg:flex fixed inset-y-0 left-0 w-64 z-30 flex-col p-3">
      <div className="glass rounded-2xl flex-1 flex flex-col p-3 overflow-hidden">
        <div className="flex items-center gap-2.5 px-2 py-2.5">
          <div className="relative size-9 rounded-xl bg-copilot-gradient grid place-items-center shadow-md">
            <Sparkles className="size-5 text-white animate-sparkle" />
          </div>
          <div className="leading-tight">
            <div className="font-semibold text-[13px] tracking-tight">Team Intelligence</div>
            <div className="text-[11px] text-muted-foreground">Change & Transformation</div>
          </div>
        </div>

        <nav className="mt-3 flex-1 overflow-y-auto no-scrollbar space-y-0.5 pr-1">
          {nav.map((item) => {
            const Icon = item.icon;
            return (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                className={({ isActive }) =>
                  cn(
                    "group flex items-center gap-3 rounded-xl px-3 py-2 text-[13px] font-medium transition-all",
                    "text-sidebar-foreground/80 hover:text-sidebar-foreground hover:bg-white/60",
                    isActive &&
                      "bg-white text-sidebar-foreground shadow-sm ring-1 ring-black/5"
                  )
                }
              >
                {({ isActive }) => (
                  <>
                    <span
                      className={cn(
                        "grid place-items-center size-7 rounded-lg transition-all",
                        isActive
                          ? "bg-copilot-gradient text-white shadow-sm"
                          : "bg-muted text-muted-foreground group-hover:bg-white"
                      )}
                    >
                      <Icon className="size-4" />
                    </span>
                    <span className="truncate">{item.label}</span>
                  </>
                )}
              </NavLink>
            );
          })}
        </nav>

        <div className="mt-3 rounded-xl p-3 ring-gradient bg-white/60">
          <div className="flex items-center gap-2 text-[12px] font-semibold">
            <Sparkles className="size-3.5 text-fuchsia-500" />
            <span className="text-gradient">Copilot ready</span>
          </div>
          <p className="text-[11px] text-muted-foreground mt-1">
            Grounded on your Initiatives, metrics, and stories.
          </p>
        </div>
      </div>
    </aside>
  );
}

export default AppSidebar;
