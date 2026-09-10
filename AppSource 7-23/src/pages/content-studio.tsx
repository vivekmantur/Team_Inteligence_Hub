import { useEffect, useMemo, useRef, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { PageHeader } from "@/components/system/PageHeader";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { cn } from "@/lib/utils";
import { Sparkles, Wand2, Copy, RefreshCcw, Linkedin, FileText, Newspaper, BookOpen, Presentation, MessageCircle, Rocket, Search, AlertTriangle, type LucideIcon } from "lucide-react";
import { useGeneratedContent } from "@/hooks/use-generated-content";
import type { GeneratedContent } from "@/data/mock";
import { useInitiativesQuery, statusToLabel, type Initiative } from "@/hooks/use-initiatives-api";
import {
  useGenerateContent,
  contentFormatToWire,
  contentToneToWire,
  contentAudienceToWire,
  contentLengthToWire,
  CONTENT_INSTRUCTIONS_MAX_LENGTH,
  type ContentGenerationRequest,
} from "@/hooks/use-content-generation";

const contentTypes: { key: GeneratedContent["type"]; icon: LucideIcon; blurb: string }[] = [
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

/**
 * One prior successful instruction/output pair in this Content Studio session — mirrors
 * the shape of ContentGenerationRequestDto.PreviousTurns' items. Kept in component-local
 * state (never the shared `memory` singleton, which deliberately survives navigation),
 * so it disappears for free when this page unmounts — e.g. on sign-out, where
 * RequireAuth unmounts routed children.
 */
type SessionTurn = { instruction: string; output: string };

export default function ContentStudioPage() {
  const [params] = useSearchParams();
  const initialType = (params.get("type") as GeneratedContent["type"]) || "LinkedIn Post";
  const [type, setType] = useState<GeneratedContent["type"]>(initialType);
  const [tone, setTone] = useState(tones[0]);
  const [audience, setAudience] = useState(audiences[0]);
  const [length, setLength] = useState(lengths[1]);
  const [initiative, setInitiative] = useState<Initiative | null>(null);
  const [instructions, setInstructions] = useState("");
  const [lastRequest, setLastRequest] = useState<
    (ContentGenerationRequest & { initiativeId: number }) | null
  >(null);
  const [sessionTurns, setSessionTurns] = useState<SessionTurn[]>([]);
  const { items, add } = useGeneratedContent();
  const { data: initiatives = [], isLoading: initiativesLoading } = useInitiativesQuery();
  const generation = useGenerateContent();

  useEffect(() => {
    if (params.get("type")) setType(params.get("type") as GeneratedContent["type"]);
  }, [params]);

  const currentMeta = useMemo(() => contentTypes.find((c) => c.key === type)!, [type]);

  const output = generation.data?.content ?? "";

  /**
   * Session identity is the selected Initiative plus the backend format value — not the
   * UI label, since that's what the request and response both carry. sessionKeyRef always
   * holds the *live* active key: it's read inside the mutation's onSuccess below, whose
   * own closure is frozen at submit time, so a late response arriving after the user has
   * since switched Initiative or format can still be detected as stale and skipped
   * (rule 16) rather than joining the wrong session.
   *
   * Writing to the ref and resetting state directly in the render body — rather than in a
   * useEffect — is the React-documented way to reset state when a derived value changes:
   * the session clears immediately, with no extra render showing stale turns first.
   */
  const sessionKeyRef = useRef<string | null>(null);
  const activeSessionKey = initiative ? `${initiative.id}:${contentFormatToWire(type)}` : null;

  if (sessionKeyRef.current !== activeSessionKey) {
    sessionKeyRef.current = activeSessionKey;
    setSessionTurns([]);
  }

  /**
   * Shared by both Generate (submits the currently-selected Initiative) and Retry
   * (replays the exact Initiative and request that just failed, which may no longer
   * match the picker) — one guard against firing while a request is already in flight,
   * one place lastRequest gets updated.
   */
  const submit = (initiativeId: number, request: ContentGenerationRequest) => {
    if (generation.isPending) return;

    const submittedRequest = { initiativeId, ...request };
    // Captured now, not read from the `instructions` field later — by the time this
    // request resolves the user may have already typed something new for the next one.
    const submittedInstruction = request.instructions ?? "";
    setLastRequest(submittedRequest);
    generation.mutate(submittedRequest, {
      onSuccess: (result) => {
        // Append exactly the one new turn — never previousTurns again — and only if this
        // response's Initiative and format still match what's actively selected right
        // now (sessionKeyRef.current is live, unlike this closure's own captured values).
        const resultSessionKey = `${result.initiativeId}:${result.format}`;
        if (sessionKeyRef.current === resultSessionKey) {
          setSessionTurns((prev) => [
            ...prev,
            { instruction: submittedInstruction, output: result.content },
          ]);
        }

        // Only recorded under the currently-selected Initiative's name — if the picker
        // has since moved on (e.g. this success came from a Retry against an older
        // selection), skip rather than label the entry with the wrong Initiative.
        if (initiative && initiative.id === initiativeId) {
          add({
            type,
            title: initiative.name.slice(0, 60),
            tone,
            audience,
            length,
            preview: result.content.slice(0, 140) + (result.content.length > 140 ? "…" : ""),
          });
        }
      },
    });
  };

  const handleGenerate = () => {
    if (!initiative) return;

    submit(initiative.id, {
      format: contentFormatToWire(type),
      tone: contentToneToWire(tone),
      audience: contentAudienceToWire(audience),
      length: contentLengthToWire(length),
      instructions: instructions.trim() || undefined,
      previousTurns: sessionTurns.length > 0 ? sessionTurns : undefined,
    });
  };

  const handleRetry = () => {
    if (!lastRequest) return;

    // Replays the exact original request — including its previousTurns exactly as they
    // were at failure time — so a retry can never duplicate the failed turn (it was
    // never appended) or drop history that existed before the failure.
    const { initiativeId, ...request } = lastRequest;
    submit(initiativeId, request);
  };

  const handleCopy = async () => {
    if (!output) return;
    try {
      await navigator.clipboard.writeText(output);
      setSessionTurns([]);
    } catch {
      // Clipboard write failed, or the API is unavailable — preserve the session and
      // show no false success.
    }
  };

  const liveStatus = generation.isPending
    ? "Generating content…"
    : generation.isError
      ? (generation.error?.message ?? "Content generation failed.")
      : generation.isSuccess
        ? "Content generated."
        : "";

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Studio"
        title="AI Content Studio"
        description="Generate executive-ready content grounded in your team's Initiatives, metrics, and stories."
      />

      {/* Screen-reader-only announcement of generation state changes — the visual states
          below (loading placeholder, output, error box) carry the same information for
          sighted users. */}
      <div role="status" aria-live="polite" className="sr-only">
        {liveStatus}
      </div>

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
            <div>
              <div className="flex items-center justify-between">
                <label className="text-[11px] uppercase tracking-wide text-muted-foreground font-semibold">
                  Instructions
                </label>
                <span className="text-[10px] text-muted-foreground">
                  {instructions.length}/{CONTENT_INSTRUCTIONS_MAX_LENGTH}
                </span>
              </div>
              <Textarea
                value={instructions}
                onChange={(e) => setInstructions(e.target.value.slice(0, CONTENT_INSTRUCTIONS_MAX_LENGTH))}
                placeholder="Anything Copilot should focus on, avoid, or keep in mind — e.g. “mention the APAC rollout” or “keep it under 100 words”…"
                rows={3}
                className="mt-1.5 rounded-xl bg-white/70 border-white/60 resize-none"
              />
            </div>
            <Button
              onClick={handleGenerate}
              disabled={generation.isPending || !initiative}
              className="w-full h-11 rounded-xl bg-copilot-gradient text-white shadow-md"
            >
              <Wand2 className="size-4" />
              {generation.isPending ? "Generating…" : "Generate with Copilot"}
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
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={handleGenerate}
                  disabled={generation.isPending}
                  className="rounded-lg"
                >
                  <RefreshCcw className="size-3.5" /> Regenerate
                </Button>
                <Button variant="ghost" size="sm" onClick={handleCopy} className="rounded-lg">
                  <Copy className="size-3.5" /> Copy
                </Button>
              </div>
            </div>

            <div className="relative mt-4">
              {generation.isPending ? (
                <div className="text-center py-16 text-muted-foreground text-sm">
                  <Sparkles className="size-6 mx-auto mb-2 text-fuchsia-500 animate-sparkle" />
                  Generating with Copilot…
                </div>
              ) : generation.isError ? (
                <div className="rounded-xl bg-rose-500/5 border border-rose-500/20 p-4">
                  <div className="flex items-start gap-2.5">
                    <AlertTriangle className="size-4 text-rose-600 shrink-0 mt-0.5" />
                    <div className="flex-1 min-w-0">
                      <div className="text-sm font-semibold text-rose-700">
                        Generation failed
                      </div>
                      <div className="text-[13px] text-muted-foreground mt-0.5">
                        {generation.error?.message || "Content generation is temporarily unavailable. Please try again."}
                      </div>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={handleRetry}
                        className="mt-3 rounded-lg"
                      >
                        <RefreshCcw className="size-3.5" /> Retry
                      </Button>
                    </div>
                  </div>
                </div>
              ) : !output ? (
                <div className="text-center py-16 text-muted-foreground text-sm">
                  <Sparkles className="size-6 mx-auto mb-2 text-fuchsia-500 animate-sparkle" />
                  Pick a format and hit <b>Generate</b> to see Copilot draft your content.
                </div>
              ) : (
                <pre className="whitespace-pre-wrap font-sans text-[14px] leading-relaxed rounded-xl bg-white/80 p-4 min-h-[280px]">
                  {output}
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
