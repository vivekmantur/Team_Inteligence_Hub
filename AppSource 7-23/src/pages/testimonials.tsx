import { useState, useMemo } from "react";
import { PageHeader } from "@/components/system/PageHeader";
import { testimonials } from "@/data/mock";
import { Button } from "@/components/ui/button";
import { Sparkles, Quote } from "lucide-react";
import { cn } from "@/lib/utils";

const audiences = ["All", "Leadership", "Stakeholder", "Customer", "Team"] as const;

export default function TestimonialsPage() {
  const [audience, setAudience] = useState<(typeof audiences)[number]>("All");
  const filtered = useMemo(
    () => testimonials.filter((t) => audience === "All" || t.audience === audience),
    [audience]
  );

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Voice of the team"
        title="Testimonials hub"
        description="Every quote from leadership, stakeholders, customers, and the team — summarized by Copilot."
        actions={
          <Button className="rounded-xl bg-copilot-gradient text-white">
            <Sparkles className="size-4" /> Summarize themes
          </Button>
        }
      />

      <div className="glass rounded-2xl p-3 flex flex-wrap gap-1.5">
        {audiences.map((a) => (
          <button
            key={a}
            onClick={() => setAudience(a)}
            className={cn(
              "px-3 h-9 rounded-xl text-[12px] font-medium transition",
              audience === a ? "bg-copilot-gradient text-white shadow-sm" : "bg-white/70 hover:bg-white"
            )}
          >
            {a}
          </button>
        ))}
      </div>

      <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-4">
        {filtered.map((t) => (
          <div key={t.id} className="glass rounded-2xl p-5 relative overflow-hidden">
            <Quote className="absolute -top-2 -right-2 size-16 text-indigo-500/10" />
            <div className="flex items-center gap-2">
              <span
                className={cn(
                  "text-[10px] font-semibold px-2 py-0.5 rounded-full",
                  t.audience === "Leadership" && "bg-indigo-500/10 text-indigo-700",
                  t.audience === "Stakeholder" && "bg-sky-500/10 text-sky-700",
                  t.audience === "Customer" && "bg-fuchsia-500/10 text-fuchsia-700",
                  t.audience === "Team" && "bg-emerald-500/10 text-emerald-700"
                )}
              >
                {t.audience}
              </span>
              <span className="text-[11px] text-muted-foreground">{t.date}</span>
            </div>
            <blockquote className="mt-3 text-[15px] leading-snug">“{t.quote}”</blockquote>
            <div className="mt-3 text-[12px]">
              <div className="font-semibold">{t.author}</div>
              <div className="text-muted-foreground">{t.role}</div>
            </div>
          </div>
        ))}
      </div>

      <div className="glass rounded-2xl p-5">
        <div className="flex items-center gap-2">
          <div className="size-8 rounded-lg bg-copilot-gradient grid place-items-center text-white">
            <Sparkles className="size-4" />
          </div>
          <h3 className="font-semibold">Copilot theme summary</h3>
        </div>
        <p className="mt-2 text-sm leading-relaxed">
          Three recurring themes across recent testimonials: <b>storytelling excellence</b>,
          <b> executive readiness</b>, and <b>role-personalized adoption</b>. Constructive
          feedback centers on deeper agent telemetry granularity.
        </p>
      </div>
    </div>
  );
}
