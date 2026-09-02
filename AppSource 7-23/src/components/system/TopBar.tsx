import { Search, Sparkles } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { NotificationsBell } from "./NotificationsBell";
import { UserMenu } from "./UserMenu";
import { useNavigate } from "react-router-dom";

export function TopBar() {
  const navigate = useNavigate();
  return (
    <header className="sticky top-0 z-20 px-4 lg:px-6 pt-3">
      <div className="glass rounded-2xl h-14 flex items-center gap-3 px-3 lg:px-4">
        <div className="relative flex-1 max-w-xl">
          <Search className="size-4 text-muted-foreground absolute left-3 top-1/2 -translate-y-1/2" />
          <Input
            placeholder="Ask Copilot or search Initiatives, stories, metrics…"
            className="h-9 pl-9 bg-white/70 border-white/60 rounded-xl focus-visible:ring-2 focus-visible:ring-indigo-300"
          />
          <kbd className="hidden md:inline-flex items-center gap-1 absolute right-2 top-1/2 -translate-y-1/2 text-[10px] text-muted-foreground bg-muted rounded-md px-1.5 py-0.5">
            ⌘ K
          </kbd>
        </div>

        <div className="ml-auto flex items-center gap-2">
          <NotificationsBell />
          <Button
            onClick={() => navigate("/copilot")}
            className="rounded-xl bg-copilot-gradient text-white shadow hover:opacity-95 h-9"
          >
            <Sparkles className="size-4" />
            <span className="hidden sm:inline">Ask Copilot</span>
          </Button>
          <UserMenu />
        </div>
      </div>
    </header>
  );
}

export default TopBar;
