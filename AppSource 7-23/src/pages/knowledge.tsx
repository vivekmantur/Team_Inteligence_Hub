import { useEffect, useMemo, useRef, useState, type MouseEvent as ReactMouseEvent } from "react";
import { useSearchParams } from "react-router-dom";
import { PageHeader } from "@/components/system/PageHeader";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import {
  Search, FileText, FileSpreadsheet, Presentation, Image as ImageIcon, Video, FileType, Upload, Plus, Loader2, Download,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useAddContribution } from "@/components/contribution/ContributionContext";
import { downloadContributionAttachment } from "@/hooks/use-contributions";

const typeMeta: Record<string, { icon: any; color: string }> = {
  PPT: { icon: Presentation, color: "from-orange-500 to-rose-500" },
  PDF: { icon: FileText, color: "from-rose-500 to-red-500" },
  Word: { icon: FileType, color: "from-indigo-500 to-blue-500" },
  Excel: { icon: FileSpreadsheet, color: "from-emerald-500 to-teal-500" },
  Image: { icon: ImageIcon, color: "from-fuchsia-500 to-purple-500" },
  Video: { icon: Video, color: "from-sky-500 to-cyan-500" },
  Other: { icon: FileText, color: "from-slate-500 to-gray-500" },
};

const filters = ["All", "PPT", "PDF", "Word", "Excel", "Image", "Video", "Other"] as const;

/** Buckets a blob's content type (falling back to its extension) into a filter category. */
function categorize(contentType: string, fileName: string): (typeof filters)[number] {
  const ct = contentType.toLowerCase();
  const ext = fileName.toLowerCase().split(".").pop() ?? "";

  if (ct === "application/pdf" || ext === "pdf") return "PDF";
  if (ct.includes("presentation") || ["ppt", "pptx"].includes(ext)) return "PPT";
  if (ct.includes("word") || ct.includes("msword") || ["doc", "docx"].includes(ext)) return "Word";
  if (ct.includes("sheet") || ct.includes("excel") || ["xls", "xlsx", "csv"].includes(ext)) return "Excel";
  if (ct.startsWith("image/")) return "Image";
  if (ct.startsWith("video/")) return "Video";
  return "Other";
}

function formatRelative(iso: string) {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return "";
  const days = Math.floor((Date.now() - date.getTime()) / (1000 * 60 * 60 * 24));
  if (days <= 0) return "today";
  if (days === 1) return "1 day ago";
  if (days < 30) return `${days} days ago`;
  const months = Math.floor(days / 30);
  return months === 1 ? "1 month ago" : `${months} months ago`;
}

type KnowledgeAsset = {
  key: string;
  attachmentId: number;
  contributionId: number;
  name: string;
  contentType: string;
  category: (typeof filters)[number];
  owner: string;
  updated: string;
  tags: string[];
  size: string;
  initiativeName: string;
};

export default function KnowledgePage() {
  const [f, setF] = useState<(typeof filters)[number]>("All");
  const [q, setQ] = useState("");
  const [openingKey, setOpeningKey] = useState<string | null>(null);
  const { openAddContribution, contributions, isLoading } = useAddContribution();
  const [searchParams, setSearchParams] = useSearchParams();
  const highlightKey = searchParams.get("highlight");
  const highlightRef = useRef<HTMLDivElement | null>(null);

  const assets: KnowledgeAsset[] = useMemo(
    () =>
      contributions.flatMap((c) =>
        c.files.map((file) => ({
          key: `${c.id}-${file.id}`,
          attachmentId: file.attachmentId,
          contributionId: file.contributionId,
          name: file.name,
          contentType: file.type,
          category: categorize(file.type, file.name),
          owner: c.submittedBy,
          updated: formatRelative(file.createdAt || c.submissionDate),
          tags: c.tags,
          size: file.size,
          initiativeName: c.initiativeName,
        }))
      ),
    [contributions]
  );

  const filtered = useMemo(
    () =>
      assets.filter(
        (a) =>
          (f === "All" || a.category === f) &&
          (q === "" ||
            a.name.toLowerCase().includes(q.toLowerCase()) ||
            a.tags.join(" ").toLowerCase().includes(q.toLowerCase()) ||
            a.initiativeName.toLowerCase().includes(q.toLowerCase()))
      ),
    [assets, f, q]
  );

  // Arriving from a Copilot citation: the target card must not be hidden behind whatever
  // filter/search happened to be active, so both are cleared as soon as a highlight is
  // requested.
  useEffect(() => {
    if (highlightKey) {
      setF("All");
      setQ("");
    }
  }, [highlightKey]);

  // Waits for the asset it targets to actually exist (contributions load async) before
  // scrolling, rather than firing once on mount and missing it. The highlight itself
  // fades after a few seconds; the `?highlight=` param is then dropped so a later
  // refresh of this same URL does not keep re-triggering the scroll.
  useEffect(() => {
    if (!highlightKey) return;
    if (!assets.some((a) => a.key === highlightKey)) return;

    const raf = requestAnimationFrame(() =>
      highlightRef.current?.scrollIntoView({ behavior: "smooth", block: "center" })
    );
    const clear = setTimeout(() => {
      setSearchParams(
        (prev) => {
          const next = new URLSearchParams(prev);
          next.delete("highlight");
          return next;
        },
        { replace: true }
      );
    }, 4000);

    return () => {
      cancelAnimationFrame(raf);
      clearTimeout(clear);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [highlightKey, assets]);

  const downloadAsset = async (a: KnowledgeAsset, e: ReactMouseEvent) => {
    e.stopPropagation();
    setOpeningKey(a.key);
    try {
      await downloadContributionAttachment(a.contributionId, {
        id: a.attachmentId,
        contributionId: a.contributionId,
        fileName: a.name,
        contentType: a.contentType,
        fileSize: 0,
        createdAt: "",
      });
    } catch (err) {
      console.error("Could not download attachment", err);
    } finally {
      setOpeningKey(null);
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Library"
        title="Knowledge Repository"
        description="Every deck, doc, workbook, image, and video — organized by Initiative and searchable across the platform."
        actions={
          <Button
            onClick={() => openAddContribution()}
            className="rounded-xl bg-copilot-gradient text-white"
          >
            <Upload className="size-4" /> Upload to Initiative
          </Button>
        }
      />

      <div className="rounded-2xl bg-gradient-to-r from-indigo-50 via-white to-fuchsia-50 border border-indigo-100 p-4 flex items-center gap-3">
        <div className="size-9 rounded-xl bg-copilot-gradient grid place-items-center text-white shadow-sm shrink-0">
          <Plus className="size-4" />
        </div>
        <div className="flex-1 min-w-0">
          <div className="text-[13px] font-semibold">Every uploaded asset belongs to an Initiative</div>
          <div className="text-[11px] text-muted-foreground">
            Uploads flow through the Add Contribution experience so every asset stays tied to an Initiative and becomes searchable, reusable, and Copilot-ready.
          </div>
        </div>
        <Button
          onClick={() => openAddContribution()}
          className="rounded-xl bg-copilot-gradient text-white shrink-0"
        >
          Upload to Initiative
        </Button>
      </div>

      <div className="glass rounded-2xl p-3 flex flex-col md:flex-row gap-3">
        <div className="relative flex-1">
          <Search className="size-4 text-muted-foreground absolute left-3 top-1/2 -translate-y-1/2" />
          <Input
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="Search Initiative Knowledge — assets, tags, owners…"
            className="h-10 pl-9 bg-white/70 rounded-xl border-white/60"
          />
        </div>
        <div className="flex gap-1.5 flex-wrap">
          {filters.map((x) => (
            <button
              key={x}
              onClick={() => setF(x)}
              className={cn(
                "px-3 h-10 rounded-xl text-[12px] font-medium transition",
                f === x ? "bg-copilot-gradient text-white shadow-sm" : "bg-white/70 hover:bg-white"
              )}
            >
              {x}
            </button>
          ))}
        </div>
      </div>

      {isLoading ? (
        <div className="glass rounded-2xl p-10 text-center text-sm text-muted-foreground">
          <Loader2 className="size-5 mx-auto mb-2 animate-spin" />
          Loading knowledge from your Initiatives…
        </div>
      ) : filtered.length === 0 ? (
        <div className="glass rounded-2xl p-10 text-center">
          <div className="mx-auto size-14 rounded-2xl bg-copilot-gradient grid place-items-center text-white shadow-md">
            <Upload className="size-6" />
          </div>
          <h3 className="mt-4 text-lg font-semibold">
            {assets.length === 0 ? "No documents yet" : "No matches"}
          </h3>
          <p className="mt-1 text-sm text-muted-foreground max-w-md mx-auto">
            {assets.length === 0
              ? "Attachments added through the Add Contribution flow will show up here automatically."
              : "Try a different search term or filter."}
          </p>
          {assets.length === 0 && (
            <Button onClick={() => openAddContribution()} className="mt-4 rounded-xl bg-copilot-gradient text-white">
              <Plus className="size-4" /> Add Contribution
            </Button>
          )}
        </div>
      ) : (
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
          {filtered.map((a) => {
            const meta = typeMeta[a.category];
            const Icon = meta.icon;
            const opening = openingKey === a.key;
            const highlighted = a.key === highlightKey;
            return (
              <div
                key={a.key}
                ref={highlighted ? highlightRef : undefined}
                className={cn(
                  "glass rounded-2xl overflow-hidden group hover:shadow-xl transition-shadow",
                  highlighted && "ring-2 ring-primary ring-offset-2"
                )}
              >
                <div className={cn("h-24 bg-gradient-to-br relative", meta.color)}>
                  <div className="absolute inset-0 bg-mesh opacity-30" />
                  <div className="absolute bottom-2 right-2 text-[10px] font-semibold text-white/90 bg-black/20 rounded-md px-2 py-0.5">
                    {a.category}
                  </div>
                  <div className="absolute top-3 left-3 size-10 rounded-xl bg-white/20 backdrop-blur grid place-items-center text-white">
                    <Icon className="size-5" />
                  </div>
                </div>
                <div className="p-4">
                  <div className="text-sm font-semibold leading-tight line-clamp-1">{a.name}</div>
                  <div className="text-[11px] text-muted-foreground mt-0.5">
                    {a.owner} · {a.updated} · {a.initiativeName}
                  </div>
                  <div className="mt-2 flex flex-wrap gap-1.5">
                    {a.tags.map((t) => (
                      <span key={t} className="text-[10px] px-2 py-0.5 rounded-full bg-white/70 border border-white/60">
                        {t}
                      </span>
                    ))}
                  </div>
                  <div className="mt-3 flex items-center justify-between text-[11px] text-muted-foreground">
                    <span>{a.size}</span>
                    <button
                      onClick={(e) => downloadAsset(a, e)}
                      disabled={opening}
                      aria-label={`Download ${a.name}`}
                      title="Download"
                      className="size-7 rounded-lg grid place-items-center text-muted-foreground hover:text-primary hover:bg-black/5 transition disabled:opacity-70"
                    >
                      {opening ? (
                        <Loader2 className="size-3.5 animate-spin" />
                      ) : (
                        <Download className="size-3.5" />
                      )}
                    </button>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
