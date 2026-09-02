import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { PageHeader } from "@/components/system/PageHeader";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import { Sparkles, Wand2, Copy, RefreshCcw, Linkedin, FileText, Newspaper, BookOpen, Presentation, MessageCircle, Rocket, Search } from "lucide-react";
import { useGeneratedContent } from "@/hooks/use-generated-content";
import type { GeneratedContent } from "@/data/mock";
import { useInitiativesQuery, statusToLabel, type Initiative } from "@/hooks/use-initiatives-api";

const contentTypes: { key: GeneratedContent["type"]; icon: any; blurb: string }[] = [
  { key: "LinkedIn Post", icon: Linkedin, blurb: "Social-ready hook + insight" },
  { key: "Newsletter", icon: Newspaper, blurb: "Internal digest for stakeholders" },
  { key: "Executive Summary", icon: FileText, blurb: "CEO-ready brief in seconds" },
  { key: "QBR Slide", icon: Presentation, blurb: "Wins, metrics, roadmap" },
  { key: "Blog", icon: BookOpen, blurb: "Long-form thought leadership" },
  { key: "Case Study", icon: FileText, blurb: "Customer Zero-driven proof" },
  { key: "Viva Engage Post", icon: MessageCircle, blurb: "Community celebration post" },
];

const tones = ["Confident", "Inspirational", "Analytical", "Story-driven", "Playful"];
const audiences = ["Leadership", "Stakeholders", "Customers", "External", "Team"];
const lengths = ["Short", "Medium", "Long"];

function craftContent(type: GeneratedContent["type"], tone: string, audience: string, length: string, initiative: Initiative | null) {
  const t = initiative?.name || "Role Hub adoption and Copilot impact this quarter";
  const opener =
    tone === "Confident"
      ? "Here's what we shipped — and why it matters."
      : tone === "Inspirational"
      ? "This is what a team of storytellers can do in 90 days."
      : tone === "Analytical"
      ? "The data tells a clear story this quarter."
      : tone === "Story-driven"
      ? "Meet the team that reclaimed Fridays for the field."
      : "Buckle up — the numbers are wild. ✨";

  const stat = "12,480 hours saved, +34.7% Copilot growth, and 128K MAU on Role Hub.";

  const closerByAudience: Record<string, string> = {
    Leadership: "Next quarter: scale APAC playbook globally and double Customer Zero output.",
    Stakeholders: "Ping us to plug this into your workstream — templates are ready.",
    Customers: "Curious how this could work for your org? Let's talk.",
    External: "Follow along as we scale this across the company. #ModernWork #Copilot",
    Team: "Huge shoutout to everyone who made this quarter unforgettable. 💜",
  };

  const bulletCore = [
    `✨ ${stat}`,
    `📈 Role Hub adoption ahead of plan in APAC and EMEA (Initiative: ${t}).`,
    `💬 Customers say: “Role Hub is the single pane of glass we needed.”`,
  ];

  const bullets = length === "Short" ? bulletCore.slice(0, 2) : length === "Long" ? [...bulletCore, `🎯 Focus for FY26: personalized adoption at scale.`, `🤝 4 new Customer Zero proof points ready to publish.`] : bulletCore;

  switch (type) {
    case "LinkedIn Post":
      return `${opener}\n\n${bullets.join("\n")}\n\n${closerByAudience[audience] ?? ""}\n\n#Copilot #ChangeManagement #Adoption`;
    case "Viva Engage Post":
      return `${opener}\n\n${bullets.join("\n")}\n\n${closerByAudience.Team}`;
    case "Newsletter":
      return `Subject: This quarter in Team Intelligence\n\nHi team,\n\n${opener} ${stat}\n\nHighlights:\n- ${bullets.join("\n- ")}\n\n${closerByAudience[audience] ?? ""}\n\n— The Adoption team`;
    case "Executive Summary":
      return `EXECUTIVE SUMMARY · ${audience.toUpperCase()}\n\nHeadline: ${opener}\n\nBy the numbers: ${stat}\n\nWhat's working: Role Hub personalization, Copilot in Field, sentiment listening.\nWhat's next: ${closerByAudience.Leadership}`;
    case "QBR Slide":
      return `QBR · FY26 Q3\n\nWins\n• ${bullets.join("\n• ")}\n\nMetrics\n• ${stat}\n\nRoadmap\n• ${closerByAudience.Leadership}`;
    case "Blog":
      return `${opener}\n\nOver the last quarter, our team leaned into a single hypothesis: personalization scales adoption. And the data agrees.\n\n${stat}\n\nIn this post, we break down three moves that unlocked results — and why we're doubling down on Customer Zero storytelling next quarter.\n\n${closerByAudience[audience] ?? ""}`;
    case "Case Study":
      return `CASE STUDY · ${t}\n\nProblem: Adoption blockers were only visible quarterly.\nSolution: Continuous listening + Copilot summarization.\nImpact: ${stat}\n\nQuote: “Role Hub is the single pane of glass we needed.” — CVP, Modern Work\n\n${closerByAudience[audience] ?? ""}`;
  }
}

export default function ContentStudioPage() {
  const [params] = useSearchParams();
  const initialType = (params.get("type") as GeneratedContent["type"]) || "LinkedIn Post";
  const [type, setType] = useState<GeneratedContent["type"]>(initialType);
  const [tone, setTone] = useState(tones[0]);
  const [audience, setAudience] = useState(audiences[0]);
  const [length, setLength] = useState(lengths[1]);
  const [initiative, setInitiative] = useState<Initiative | null>(null);
  const [output, setOutput] = useState<string>("");
  const [generating, setGenerating] = useState(false);
  const { items, add } = useGeneratedContent();
  const { data: initiatives = [], isLoading: initiativesLoading } = useInitiativesQuery();

  useEffect(() => {
    if (params.get("type")) setType(params.get("type") as GeneratedContent["type"]);
  }, [params]);

  const currentMeta = useMemo(() => contentTypes.find((c) => c.key === type)!, [type]);

  const handleGenerate = () => {
    if (!initiative) return;
    setGenerating(true);
    setOutput("");
    const full = craftContent(type, tone, audience, length, initiative);
    let i = 0;
    const timer = setInterval(() => {
      i += Math.max(2, Math.round(full.length / 60));
      setOutput(full.slice(0, i));
      if (i >= full.length) {
        clearInterval(timer);
        setGenerating(false);
        add({
          type,
          title: initiative.name.slice(0, 60),
          tone,
          audience,
          length,
          preview: full.slice(0, 140) + "…",
        });
      }
    }, 30);
  };

  const handleCopy = () => {
    if (!output) return;
    navigator.clipboard?.writeText(output).catch(() => {});
  };

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Studio"
        title="AI Content Studio"
        description="Generate executive-ready content grounded in your team's Initiatives, metrics, and stories."
      />

      <div className="grid xl:grid-cols-[1.1fr_1.4fr] gap-4">
        {/* Config */}
        <div className="space-y-4">
          <div className="glass rounded-2xl p-5">
            <h3 className="font-semibold mb-3">Choose format</h3>
            <div className="grid grid-cols-2 gap-2">
              {contentTypes.map((c) => {
                const Icon = c.icon;
                const active = c.key === type;
                return (
                  <button
                    key={c.key}
                    onClick={() => setType(c.key)}
                    className={cn(
                      "text-left rounded-xl p-3 border transition",
                      active
                        ? "bg-copilot-gradient text-white border-transparent shadow-md"
                        : "bg-white/70 border-white/60 hover:bg-white"
                    )}
                  >
                    <div className="flex items-center gap-2">
                      <div
                        className={cn(
                          "size-8 rounded-lg grid place-items-center",
                          active ? "bg-white/20 text-white" : "bg-muted text-foreground"
                        )}
                      >
                        <Icon className="size-4" />
                      </div>
                      <div>
                        <div className="text-[13px] font-semibold">{c.key}</div>
                        <div className={cn("text-[11px]", active ? "text-white/80" : "text-muted-foreground")}>
                          {c.blurb}
                        </div>
                      </div>
                    </div>
                  </button>
                );
              })}
            </div>
          </div>

          <div className="glass rounded-2xl p-5 space-y-4">
            <h3 className="font-semibold">Tune the output</h3>
            <Selector label="Tone" value={tone} options={tones} onChange={setTone} />
            <Selector label="Audience" value={audience} options={audiences} onChange={setAudience} />
            <Selector label="Length" value={length} options={lengths} onChange={setLength} />
            <div>
              <label className="text-[11px] uppercase tracking-wide text-muted-foreground font-semibold">
                Ground on Initiative
              </label>
              <InitiativePickerInput
                initiatives={initiatives}
                loading={initiativesLoading}
                value={initiative}
                onChange={setInitiative}
                className="mt-1.5"
              />
            </div>
            <Button
              onClick={handleGenerate}
              disabled={generating || !initiative}
              className="w-full h-11 rounded-xl bg-copilot-gradient text-white shadow-md"
            >
              <Wand2 className="size-4" />
              {generating ? "Generating…" : "Generate with Copilot"}
            </Button>
          </div>
        </div>

        {/* Output */}
        <div className="space-y-4">
          <div className="glass rounded-2xl p-5 min-h-[420px] relative overflow-hidden">
            <div className="absolute inset-0 bg-mesh opacity-40 pointer-events-none" />
            <div className="relative flex items-center justify-between">
              <div className="inline-flex items-center gap-2">
                <div className="size-8 rounded-lg bg-copilot-gradient text-white grid place-items-center">
                  <Sparkles className="size-4" />
                </div>
                <div>
                  <div className="text-sm font-semibold">{currentMeta.key} · {tone}</div>
                  <div className="text-[11px] text-muted-foreground">For {audience} · {length}</div>
                </div>
              </div>
              <div className="flex gap-1.5">
                <Button variant="ghost" size="sm" onClick={handleGenerate} className="rounded-lg">
                  <RefreshCcw className="size-3.5" /> Regenerate
                </Button>
                <Button variant="ghost" size="sm" onClick={handleCopy} className="rounded-lg">
                  <Copy className="size-3.5" /> Copy
                </Button>
              </div>
            </div>

            <div className="relative mt-4">
              {!output && !generating ? (
                <div className="text-center py-16 text-muted-foreground text-sm">
                  <Sparkles className="size-6 mx-auto mb-2 text-fuchsia-500 animate-sparkle" />
                  Pick a format and hit <b>Generate</b> to see Copilot draft your content.
                </div>
              ) : (
                <pre className="whitespace-pre-wrap font-sans text-[14px] leading-relaxed rounded-xl bg-white/80 p-4 min-h-[280px]">
{output}
                  {generating && <span className="inline-block w-1.5 h-4 bg-primary/70 animate-pulse ml-0.5 align-middle" />}
                </pre>
              )}
            </div>
          </div>

          <div className="glass rounded-2xl p-5">
            <div className="flex items-center justify-between mb-3">
              <h3 className="font-semibold">Recent generations</h3>
              <span className="text-[11px] text-muted-foreground">{items.length} items</span>
            </div>
            <ul className="space-y-2">
              {items.slice(0, 6).map((g) => (
                <li key={g.id} className="rounded-xl bg-white/60 p-3">
                  <div className="flex items-center justify-between">
                    <div className="text-[11px] font-semibold text-gradient uppercase tracking-wide">{g.type}</div>
                    <div className="text-[11px] text-muted-foreground">{g.createdAt}</div>
                  </div>
                  <div className="text-sm font-medium mt-0.5 truncate">{g.title || g.type}</div>
                  <div className="text-[12px] text-muted-foreground mt-1 line-clamp-2">{g.preview}</div>
                </li>
              ))}
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
}

/**
 * A search-to-filter dropdown over the real Initiatives list, rather than a plain
 * <select> — the list can grow past what's comfortable to scan, and typing a few
 * letters of the name is faster than scrolling once there are more than a handful.
 */
function InitiativePickerInput({
  initiatives,
  loading,
  value,
  onChange,
  className,
}: {
  initiatives: Initiative[];
  loading: boolean;
  value: Initiative | null;
  onChange: (v: Initiative | null) => void;
  className?: string;
}) {
  const [q, setQ] = useState("");
  const [open, setOpen] = useState(false);

  const matches = useMemo(() => {
    const term = q.trim().toLowerCase();
    const pool = term
      ? initiatives.filter((i) => i.name.toLowerCase().includes(term))
      : initiatives;
    return pool.slice(0, 8);
  }, [initiatives, q]);

  return (
    <div className={cn("relative", className)}>
      <div className="relative">
        <Search className="size-4 text-muted-foreground absolute left-3 top-1/2 -translate-y-1/2" />
        <Input
          value={open ? q : value?.name ?? q}
          onChange={(e) => {
            setQ(e.target.value);
            if (value) onChange(null);
            setOpen(true);
          }}
          onFocus={() => {
            setQ("");
            setOpen(true);
          }}
          onBlur={() => setTimeout(() => setOpen(false), 120)}
          placeholder={loading ? "Loading Initiatives…" : "Search an Initiative to ground this content on…"}
          disabled={loading}
          className="h-10 pl-9 rounded-xl bg-white/70 border-white/60"
        />
      </div>
      {open && matches.length > 0 && (
        <div className="absolute z-20 mt-1 w-full rounded-xl bg-white border border-black/5 shadow-xl overflow-hidden">
          {matches.map((i) => (
            <button
              key={i.id}
              type="button"
              onMouseDown={(e) => e.preventDefault()}
              onClick={() => {
                onChange(i);
                setQ("");
                setOpen(false);
              }}
              className="w-full text-left px-3 py-2 hover:bg-muted flex items-center gap-2"
            >
              <div className="size-7 rounded-lg bg-gradient-to-br from-indigo-500/15 to-fuchsia-500/15 grid place-items-center shrink-0">
                <Rocket className="size-3.5 text-indigo-600" />
              </div>
              <div className="min-w-0">
                <div className="text-[13px] font-medium truncate">{i.name}</div>
                <div className="text-[11px] text-muted-foreground truncate">
                  {i.businessArea} · {statusToLabel(i.status)}
                </div>
              </div>
            </button>
          ))}
        </div>
      )}
      {open && q && matches.length === 0 && (
        <div className="absolute z-20 mt-1 w-full rounded-xl bg-white border border-black/5 shadow-xl px-3 py-2 text-[13px] text-muted-foreground">
          No Initiatives match "{q}".
        </div>
      )}
    </div>
  );
}

function Selector({
  label,
  value,
  options,
  onChange,
}: {
  label: string;
  value: string;
  options: string[];
  onChange: (v: string) => void;
}) {
  return (
    <div>
      <label className="text-[11px] uppercase tracking-wide text-muted-foreground font-semibold">{label}</label>
      <div className="mt-1.5 flex flex-wrap gap-1.5">
        {options.map((o) => (
          <button
            key={o}
            onClick={() => onChange(o)}
            className={cn(
              "px-3 h-8 rounded-lg text-[12px] font-medium transition",
              value === o ? "bg-copilot-gradient text-white shadow-sm" : "bg-white/70 hover:bg-white"
            )}
          >
            {o}
          </button>
        ))}
      </div>
    </div>
  );
}
