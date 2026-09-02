import { useMemo, useRef, useState } from "react";
import { teamMembers } from "@/data/mock";
import { cn } from "@/lib/utils";
import { initials } from "./PeoplePicker";

/** Somebody who can be mentioned. */
export type MentionPerson = { id: number; name: string };

interface MentionInputProps {
  value: string;
  onChange: (v: string) => void;
  onMentionsChange?: (names: string[]) => void;
  placeholder?: string;
  className?: string;
  rows?: number;
  /**
   * Real people to suggest. Falls back to mock data when omitted, so callers that are
   * not yet wired to the API keep working unchanged.
   */
  people?: MentionPerson[];
  /** Fires with the ids of everyone mentioned. Only meaningful alongside `people`. */
  onMentionedIdsChange?: (ids: number[]) => void;
}

const MENTION_REGEX = /@([\w][\w '.-]*)/g;

export function extractMentions(text: string, poolNames: string[]): string[] {
  const found = new Set<string>();
  const lowerPool = new Map(poolNames.map((n) => [n.toLowerCase(), n]));
  let match: RegExpExecArray | null;
  MENTION_REGEX.lastIndex = 0;
  while ((match = MENTION_REGEX.exec(text)) !== null) {
    const q = match[1].trim().toLowerCase();
    // Longest-name-first check
    const sortedPool = Array.from(lowerPool.keys()).sort((a, b) => b.length - a.length);
    const hit = sortedPool.find((n) => q.startsWith(n) || n.startsWith(q));
    if (hit && q === hit) found.add(lowerPool.get(hit)!);
  }
  // Also handle full-word matches (e.g. "@Priya Menon") by scanning the string for @<name>
  for (const name of poolNames) {
    const needle = `@${name}`;
    if (text.toLowerCase().includes(needle.toLowerCase())) found.add(name);
  }
  return Array.from(found);
}

export function MentionInput({
  value,
  onChange,
  onMentionsChange,
  placeholder,
  className,
  rows = 3,
  people,
  onMentionedIdsChange,
}: MentionInputProps) {
  const ref = useRef<HTMLTextAreaElement>(null);
  const [openAt, setOpenAt] = useState<number | null>(null);
  const [query, setQuery] = useState("");

  // Real users when supplied, mock names otherwise.
  const candidates = useMemo(
    () => people ?? teamMembers.map((m) => ({ id: -1, name: m.name })),
    [people]
  );

  const pool = useMemo(() => candidates.map((p) => p.name), [candidates]);

  const suggestions = useMemo(() => {
    if (openAt === null) return [];
    const q = query.toLowerCase();
    return candidates.filter((p) => p.name.toLowerCase().includes(q)).slice(0, 5);
  }, [openAt, query, candidates]);

  const notifyMentions = (text: string) => {
    const names = extractMentions(text, pool);
    onMentionsChange?.(names);

    // Names are what the writer typed; ids are what the database stores.
    onMentionedIdsChange?.(
      names
        .map((name) => candidates.find((p) => p.name === name)?.id)
        .filter((id): id is number => typeof id === "number" && id > 0)
    );
  };

  const handleChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    const text = e.target.value;
    onChange(text);
    notifyMentions(text);

    const caret = e.target.selectionStart ?? text.length;
    // Look backwards from caret to find @
    const upto = text.slice(0, caret);
    const at = upto.lastIndexOf("@");
    if (at >= 0) {
      const between = upto.slice(at + 1);
      // Only open if no space right after @
      if (!/\s{2,}/.test(between) && between.length <= 30) {
        setOpenAt(at);
        setQuery(between);
        return;
      }
    }
    setOpenAt(null);
    setQuery("");
  };

  const insertMention = (name: string) => {
    if (openAt === null || !ref.current) return;
    const el = ref.current;
    const caret = el.selectionStart ?? value.length;
    const before = value.slice(0, openAt);
    const after = value.slice(caret);
    const newValue = `${before}@${name} ${after}`;
    onChange(newValue);
    notifyMentions(newValue);
    setOpenAt(null);
    setQuery("");
    requestAnimationFrame(() => {
      el.focus();
      const pos = (before + `@${name} `).length;
      el.setSelectionRange(pos, pos);
    });
  };

  return (
    <div className={cn("relative", className)}>
      <textarea
        ref={ref}
        value={value}
        onChange={handleChange}
        placeholder={placeholder}
        rows={rows}
        className="w-full rounded-xl bg-white/80 border border-white/70 px-3 py-2 text-sm resize-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-indigo-300"
      />
      {openAt !== null && suggestions.length > 0 && (
        <div className="absolute z-30 left-2 bottom-full mb-1 w-64 rounded-xl bg-white border border-black/5 shadow-xl overflow-hidden">
          <div className="px-3 py-1.5 text-[10px] uppercase tracking-wider text-muted-foreground bg-muted/60">
            Mention a teammate
          </div>
          {suggestions.map((m) => (
            <button
              key={m.id}
              type="button"
              onMouseDown={(e) => e.preventDefault()}
              onClick={() => insertMention(m.name)}
              className="w-full text-left px-3 py-2 hover:bg-muted flex items-center gap-2"
            >
              <div
                className={cn(
                  "size-6 rounded-full text-white text-[10px] font-semibold grid place-items-center bg-gradient-to-br",
                  "avatarColor" in m ? (m.avatarColor as string) : "from-indigo-500 to-fuchsia-500"
                )}
              >
                {initials(m.name)}
              </div>
              <div className="min-w-0">
                <div className="text-[12px] font-medium truncate">{m.name}</div>
                {"role" in m && (
                  <div className="text-[10px] text-muted-foreground truncate">
                    {m.role as string}
                  </div>
                )}
              </div>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

/**
 * Splits text into plain runs and mention chips.
 *
 * @param poolNames Names to recognise. Defaults to mock data so callers that are not
 *                  yet wired to the API keep working; pass real names to highlight them.
 */
export function renderMentionedText(text: string, poolNames?: string[]) {
  const parts: (string | { name: string })[] = [];
  let lastIndex = 0;
  const names = poolNames ?? teamMembers.map((m) => m.name);
  // Split by any @Name occurrence
  const sortedNames = [...names].sort((a, b) => b.length - a.length);
  let cursor = 0;
  while (cursor < text.length) {
    let matched = false;
    if (text[cursor] === "@") {
      for (const n of sortedNames) {
        const slice = text.slice(cursor + 1, cursor + 1 + n.length);
        if (slice.toLowerCase() === n.toLowerCase()) {
          if (cursor > lastIndex) parts.push(text.slice(lastIndex, cursor));
          parts.push({ name: n });
          cursor += n.length + 1;
          lastIndex = cursor;
          matched = true;
          break;
        }
      }
    }
    if (!matched) cursor++;
  }
  if (lastIndex < text.length) parts.push(text.slice(lastIndex));
  return parts;
}
