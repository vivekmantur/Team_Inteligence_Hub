import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { PageHeader } from "@/components/system/PageHeader";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
} from "@/components/ui/sheet";
import { Sparkles, Send, Paperclip, Zap, FileText, ChevronRight } from "lucide-react";
import { cn } from "@/lib/utils";
import { ApiError } from "@/lib/api-client";
import { useAskCopilot, type CopilotCitation } from "@/hooks/use-copilot";
import { useAddContribution } from "@/components/contribution/ContributionContext";

/**
 * Reduces a filename to the same alphanumeric-only, lowercased slug the backend derives
 * when it names a blob (see AzureBlobFileStorage.SanitizeForBlobName) — the extension and
 * any GUID-prefix (`{32 hex}_`) Azure AI Search's indexer may have picked up as the title
 * are stripped first, so a citation and a real attachment line up whenever they name the
 * same file, even though one comes from the search index and the other from the API.
 */
function slugForMatch(raw: string): string {
  return raw
    .replace(/\.[^./]+$/, "")
    .replace(/^[0-9a-f]{32}_/i, "")
    .replace(/[^a-zA-Z0-9_-]/g, "")
    .toLowerCase();
}

const suggestions = [
  "Generate my QBR",
  "What were our biggest accomplishments this quarter?",
  "Create a LinkedIn post about Role Hub adoption",
  "Summarize customer feedback",
  "Show top success stories",
  "Create executive talking points for leadership",
];

interface Msg {
  id: string;
  role: "user" | "assistant";
  content: string;
  streaming?: boolean;
  error?: boolean;
  citations?: CopilotCitation[];
  suggestedQuestions?: string[];
}

export default function CopilotPage() {
  const [params] = useSearchParams();
  const initialPrompt = params.get("prompt") || "";
  const [input, setInput] = useState(initialPrompt);
  const [messages, setMessages] = useState<Msg[]>([]);
  const [streaming, setStreaming] = useState(false);
  const scrollRef = useRef<HTMLDivElement | null>(null);
  const askCopilot = useAskCopilot();
  const navigate = useNavigate();
  /** Which message's sources panel is open, if any — null means the panel is closed. */
  const [sourcesFor, setSourcesFor] = useState<Msg | null>(null);

  /**
   * Every real attachment across every Initiative, indexed by its slug — the same list
   * Knowledge Repository renders. Lets a citation resolve to "the actual file at
   * /knowledge?highlight=<key>" instead of just naming it.
   */
  const { contributions } = useAddContribution();
  const filesBySlug = useMemo(() => {
    const map = new Map<string, { key: string; name: string }>();
    for (const c of contributions) {
      for (const file of c.files) {
        map.set(slugForMatch(file.name), { key: `${c.id}-${file.id}`, name: file.name });
      }
    }
    return map;
  }, [contributions]);

  const resolveCitation = (c: CopilotCitation) => {
    const raw = c.title || c.source || "";
    return filesBySlug.get(slugForMatch(raw)) ?? null;
  };

  /**
   * Types the finished answer out a few characters at a time. The API returns the whole
   * answer in one response — there is no token stream to consume yet — so this is purely
   * a UI pace-out, run only after the real network round trip has already completed.
   */
  const revealAnswer = (
    assistantId: string,
    full: string,
    citations?: CopilotCitation[],
    suggestedQuestions?: string[]
  ) => {
    let i = 0;
    const timer = setInterval(() => {
      i += Math.max(2, Math.round(full.length / 80));
      setMessages((m) =>
        m.map((msg) => (msg.id === assistantId ? { ...msg, content: full.slice(0, i) } : msg))
      );
      if (i >= full.length) {
        clearInterval(timer);
        setMessages((m) =>
          m.map((msg) =>
            msg.id === assistantId
              ? { ...msg, content: full, streaming: false, citations, suggestedQuestions }
              : msg
          )
        );
        setStreaming(false);
      }
    }, 25);
  };

  const send = async (text?: string) => {
    const content = (text ?? input).trim();
    if (!content || streaming) return;
    const userMsg: Msg = { id: crypto.randomUUID(), role: "user", content };
    const assistantId = crypto.randomUUID();
    setMessages((m) => [...m, userMsg, { id: assistantId, role: "assistant", content: "", streaming: true }]);
    setInput("");
    setStreaming(true);

    try {
      const result = await askCopilot.mutateAsync(content);
      revealAnswer(assistantId, result.answer, result.citations, result.suggestedQuestions);
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : "Something went wrong reaching Copilot. Please try again.";
      setMessages((m) =>
        m.map((msg) =>
          msg.id === assistantId ? { ...msg, content: message, streaming: false, error: true } : msg
        )
      );
      setStreaming(false);
    }
  };

  useEffect(() => {
    if (initialPrompt) {
      send(initialPrompt);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: "smooth" });
  }, [messages]);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Copilot"
        title="AI Copilot Assistant"
        description="Ask anything about your Initiatives, metrics, and stories. I'm grounded on every table in the platform."
      />

      <div className="glass rounded-2xl overflow-hidden relative">
        <div className="absolute inset-0 bg-mesh opacity-40 pointer-events-none" />
        <div ref={scrollRef} className="relative max-h-[52vh] min-h-[320px] overflow-y-auto p-5 space-y-4">
          {messages.length === 0 ? (
            <div className="text-center py-10">
              <div className="size-14 mx-auto rounded-2xl bg-copilot-gradient grid place-items-center shadow-lg">
                <Sparkles className="size-7 text-white animate-sparkle" />
              </div>
              <h3 className="mt-3 text-lg font-semibold">How can I help you today?</h3>
              <p className="text-sm text-muted-foreground max-w-md mx-auto">
                I can generate QBRs, draft posts, summarize feedback, and surface success stories.
              </p>
            </div>
          ) : (
            messages.map((m) => (
              <div key={m.id} className={cn("flex gap-3", m.role === "user" && "justify-end")}>
                {m.role === "assistant" && (
                  <div className="size-8 rounded-lg bg-copilot-gradient text-white grid place-items-center shrink-0">
                    <Sparkles className="size-4" />
                  </div>
                )}
                <div
                  className={cn(
                    "max-w-[80%] rounded-2xl px-4 py-3 text-[14px] leading-relaxed whitespace-pre-wrap",
                    m.role === "user"
                      ? "bg-copilot-gradient text-white shadow"
                      : m.error
                        ? "bg-red-50/90 border border-red-200 text-red-800"
                        : "bg-white/85 border border-white/60"
                  )}
                >
                  {m.content}
                  {m.streaming && (
                    <span className="inline-block w-1.5 h-4 bg-primary/70 animate-pulse ml-0.5 align-middle" />
                  )}
                  {m.citations && m.citations.length > 0 && (
                    <div className="mt-3 pt-2 border-t border-black/5">
                      <button
                        onClick={() => setSourcesFor(m)}
                        className="text-[11px] px-2.5 py-1.5 rounded-full bg-black/5 hover:bg-black/10 transition inline-flex items-center gap-1.5 font-medium"
                      >
                        <FileText className="size-3 shrink-0 text-muted-foreground" />
                        Sources &middot; {m.citations.length}
                        <ChevronRight className="size-3 shrink-0 text-muted-foreground" />
                      </button>
                    </div>
                  )}
                  {m.suggestedQuestions && m.suggestedQuestions.length > 0 && (
                    <div className="mt-3 pt-2 border-t border-black/5 flex flex-col gap-1.5 items-start">
                      {m.suggestedQuestions.map((q, idx) => (
                        <button
                          key={idx}
                          onClick={() => send(q)}
                          disabled={streaming}
                          className="text-[12px] text-left px-2.5 py-1.5 rounded-lg bg-white/80 border border-fuchsia-200/70 hover:bg-white transition inline-flex items-center gap-1.5 disabled:opacity-50"
                        >
                          <Zap className="size-3 shrink-0 text-fuchsia-500" />
                          {q}
                        </button>
                      ))}
                    </div>
                  )}
                </div>
                {m.role === "user" && (
                  <div className="size-8 rounded-full bg-gradient-to-br from-indigo-500 to-fuchsia-500 text-white grid place-items-center text-xs font-semibold shrink-0">
                    NP
                  </div>
                )}
              </div>
            ))
          )}
        </div>

        <div className="relative border-t border-white/60 bg-white/60 p-3">
          <div className="flex flex-wrap gap-1.5 mb-2">
            {suggestions.map((s) => (
              <button
                key={s}
                onClick={() => send(s)}
                className="text-[11px] px-2.5 py-1 rounded-full bg-white/80 border border-white/70 hover:bg-white transition inline-flex items-center gap-1"
              >
                <Zap className="size-3 text-fuchsia-500" />
                {s}
              </button>
            ))}
          </div>
          <div className="flex items-end gap-2">
            <Button variant="ghost" size="icon" className="rounded-xl shrink-0" aria-label="Attach">
              <Paperclip className="size-4" />
            </Button>
            <Textarea
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter" && !e.shiftKey) {
                  e.preventDefault();
                  send();
                }
              }}
              rows={1}
              placeholder="Ask Copilot anything about your team…"
              className="rounded-xl bg-white/90 border-white/70 resize-none min-h-[44px]"
            />
            <Button
              onClick={() => send()}
              disabled={streaming}
              className="rounded-xl bg-copilot-gradient text-white h-11 px-4"
            >
              <Send className="size-4" />
            </Button>
          </div>
        </div>
      </div>

      <Sheet open={sourcesFor !== null} onOpenChange={(open) => !open && setSourcesFor(null)}>
        <SheetContent side="right" className="w-full sm:max-w-md flex flex-col p-0">
          <SheetHeader className="border-b border-border">
            <SheetTitle>Sources</SheetTitle>
            <SheetDescription>
              {sourcesFor?.citations?.length ?? 0}{" "}
              {(sourcesFor?.citations?.length ?? 0) === 1 ? "document" : "documents"} used to
              ground this answer
            </SheetDescription>
          </SheetHeader>
          <div className="flex-1 overflow-y-auto p-4 space-y-2">
            {sourcesFor?.citations?.map((c, idx) => {
              const match = resolveCitation(c);
              const label = match?.name || c.title || c.source || `Source ${idx + 1}`;

              const row = (
                <>
                  <div className="size-7 rounded-lg bg-copilot-gradient text-white grid place-items-center shrink-0 text-[11px] font-semibold">
                    {idx + 1}
                  </div>
                  <div className="min-w-0 flex-1 text-sm font-medium break-words">{label}</div>
                  {match && <ChevronRight className="size-4 shrink-0 text-muted-foreground" />}
                </>
              );

              return match ? (
                <button
                  key={idx}
                  onClick={() => {
                    setSourcesFor(null);
                    navigate(`/knowledge?highlight=${match.key}`);
                  }}
                  className="w-full flex items-center gap-2.5 rounded-xl border border-border bg-muted/40 p-3 hover:bg-muted hover:border-primary/40 transition text-left"
                >
                  {row}
                </button>
              ) : (
                <div
                  key={idx}
                  className="flex items-center gap-2.5 rounded-xl border border-border bg-muted/40 p-3 opacity-70"
                >
                  {row}
                </div>
              );
            })}
          </div>
        </SheetContent>
      </Sheet>
    </div>
  );
}
