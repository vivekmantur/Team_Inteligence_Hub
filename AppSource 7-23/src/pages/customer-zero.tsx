import { PageHeader } from "@/components/system/PageHeader";
import { customerZero } from "@/data/mock";
import { Sparkles, Target, Quote } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export default function CustomerZeroPage() {
  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Storytelling"
        title="Customer Zero stories"
        description="Problem, solution, impact — every story ready to become a case study or brief."
        actions={
          <Button className="rounded-xl bg-copilot-gradient text-white">
            <Sparkles className="size-4" /> Auto-draft case study
          </Button>
        }
      />

      <div className="grid lg:grid-cols-2 gap-4">
        {customerZero.map((s) => (
          <div key={s.id} className="glass rounded-2xl p-5 relative overflow-hidden">
            <div className="absolute -top-20 -right-16 size-56 rounded-full bg-copilot-gradient opacity-15 blur-3xl" />
            <div className="flex items-center justify-between relative">
              <div className="inline-flex items-center gap-2 text-[11px] font-semibold">
                <Target className="size-3.5 text-fuchsia-500" />
                <span className="text-gradient uppercase tracking-[0.14em]">{s.vertical}</span>
              </div>
              <span
                className={cn(
                  "text-[10px] font-semibold px-2 py-0.5 rounded-full",
                  s.status === "Published" && "bg-emerald-500/10 text-emerald-700",
                  s.status === "In Review" && "bg-amber-500/10 text-amber-700",
                  s.status === "Draft" && "bg-slate-500/10 text-slate-700"
                )}
              >
                {s.status}
              </span>
            </div>
            <h3 className="mt-2 text-lg font-semibold leading-tight relative">{s.title}</h3>

            <div className="mt-3 grid md:grid-cols-3 gap-2 relative">
              <div className="rounded-xl bg-white/60 p-3">
                <div className="text-[10px] font-semibold text-rose-600 uppercase tracking-wide">Problem</div>
                <div className="text-sm mt-1 leading-snug">{s.problem}</div>
              </div>
              <div className="rounded-xl bg-white/60 p-3">
                <div className="text-[10px] font-semibold text-indigo-600 uppercase tracking-wide">Solution</div>
                <div className="text-sm mt-1 leading-snug">{s.solution}</div>
              </div>
              <div className="rounded-xl bg-white/60 p-3">
                <div className="text-[10px] font-semibold text-emerald-600 uppercase tracking-wide">Impact</div>
                <div className="text-sm mt-1 leading-snug">{s.impact}</div>
              </div>
            </div>

            <div className="mt-3 flex items-center justify-between relative">
              <div className="inline-flex items-center gap-2 text-sm font-semibold">
                <span className="px-2 py-0.5 rounded-lg bg-copilot-gradient text-white text-xs">{s.metric}</span>
              </div>
            </div>

            <blockquote className="mt-3 rounded-xl bg-white/70 p-3 relative">
              <Quote className="size-3.5 text-fuchsia-500 mb-1" />
              <div className="text-sm italic">“{s.quote}”</div>
              <div className="text-[11px] text-muted-foreground mt-1">— {s.quoteAuthor}</div>
            </blockquote>
          </div>
        ))}
      </div>
    </div>
  );
}
