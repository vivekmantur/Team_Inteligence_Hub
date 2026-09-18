import { useMemo, useState } from "react";
import { PageHeader } from "@/components/system/PageHeader";
import { Button } from "@/components/ui/button";
import { Sparkles, Target, MessageSquareQuote, FileText } from "lucide-react";
import { cn } from "@/lib/utils";
import {
  useCustomerStories,
  useTestimonials,
  useDocumentCustomerStories,
  useDocumentTestimonials,
  type CustomerStoryCardRecord,
  type TestimonialCardRecord,
  type DocumentCustomerStoryCardRecord,
  type DocumentTestimonialCardRecord,
} from "@/hooks/use-contributions";

type Category = "All" | "Customer Zero Story" | "Testimonial";

const categories: Category[] = ["All", "Customer Zero Story", "Testimonial"];

/**
 * One shape both source types map onto, so the grid can render either kind of proof
 * point with the same card without the page needing two separate loops.
 */
interface StoryCard {
  id: string;
  category: Exclude<Category, "All">;
  badge: string;
  badgeTone: string;
  title: string;
  description: string;
  footer: string;
}

const customerBadgeTone = "bg-indigo-500/10 text-indigo-700";

const sentimentTone: Record<string, string> = {
  Positive: "bg-emerald-500/10 text-emerald-700",
  Neutral: "bg-slate-500/10 text-slate-700",
  Constructive: "bg-amber-500/10 text-amber-700",
};

function buildCards(
  customerStories: CustomerStoryCardRecord[],
  testimonials: TestimonialCardRecord[]
): StoryCard[] {
  const fromCustomerZero: StoryCard[] = customerStories.map((s) => ({
    id: `cz-${s.id}`,
    category: "Customer Zero Story",
    badge: s.customerName,
    badgeTone: customerBadgeTone,
    title: s.title,
    description: s.summary ?? s.quote ?? s.businessValue ?? "",
    footer: s.outcome ?? s.keyTakeaway ?? s.customerName,
  }));

  const fromTestimonials: StoryCard[] = testimonials.map((t) => ({
    id: `ts-${t.id}`,
    category: "Testimonial",
    badge: t.sentiment,
    badgeTone: sentimentTone[t.sentiment],
    title: t.speakerName,
    description: `“${t.quote}”`,
    footer: [t.speakerRole, t.audience].filter(Boolean).join(" · "),
  }));

  return [...fromCustomerZero, ...fromTestimonials];
}

/**
 * Same StoryCard shape as buildCards, so the "Extracted from documents" section can reuse
 * the existing card rendering rather than a second layout. Badges and footers lean on the
 * source file name where the extracted row is missing a field a hand-written one always
 * has (no contributor title, and CustomerName/SpeakerName/Quote are all nullable here).
 */
function buildDocumentCards(
  customerStories: DocumentCustomerStoryCardRecord[],
  testimonials: DocumentTestimonialCardRecord[]
): StoryCard[] {
  const fromCustomerZero: StoryCard[] = customerStories.map((s) => ({
    id: `doc-cz-${s.id}`,
    category: "Customer Zero Story",
    badge: s.customerName ?? "Customer",
    badgeTone: customerBadgeTone,
    title: s.customerName ?? s.sourceFileName,
    description: s.summary ?? s.quote ?? s.businessValue ?? "",
    footer: s.outcome ?? `From ${s.sourceFileName}`,
  }));

  const fromTestimonials: StoryCard[] = testimonials.map((t) => ({
    id: `doc-ts-${t.id}`,
    category: "Testimonial",
    badge: t.sentiment ?? "Testimonial",
    badgeTone: t.sentiment ? sentimentTone[t.sentiment] : sentimentTone.Neutral,
    title: t.speakerName ?? t.sourceFileName,
    description: t.quote ? `"${t.quote}"` : "",
    footer: [t.speakerRole, t.audience].filter(Boolean).join(" · ") || `From ${t.sourceFileName}`,
  }));

  return [...fromCustomerZero, ...fromTestimonials];
}

/** Shared grid used by both the main library and the "Extracted from documents" section. */
function StoryCardGrid({
  cards,
  isLoading,
  emptyTitle,
  emptyDescription,
  documentSourced = false,
}: {
  cards: StoryCard[];
  isLoading: boolean;
  emptyTitle: string;
  emptyDescription: string;
  documentSourced?: boolean;
}) {
  if (isLoading) {
    return (
      <div className="glass rounded-2xl p-8 text-center text-sm text-muted-foreground">
        Loading stories…
      </div>
    );
  }

  if (cards.length === 0) {
    return (
      <div className="glass rounded-2xl p-8 text-center">
        <h3 className="text-lg font-semibold">{emptyTitle}</h3>
        <p className="mt-1 text-sm text-muted-foreground max-w-md mx-auto">
          {emptyDescription}
        </p>
      </div>
    );
  }

  return (
    <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-4">
      {cards.map((c) => {
        const Icon = documentSourced
          ? FileText
          : c.category === "Customer Zero Story"
            ? Target
            : MessageSquareQuote;
        return (
          <div key={c.id} className="glass rounded-2xl p-5">
            <div className="flex items-start justify-between">
              <div className="size-8 rounded-lg bg-gradient-to-br from-indigo-500/15 to-fuchsia-500/15 grid place-items-center text-indigo-600 shrink-0">
                <Icon className="size-4" />
              </div>
              <span className={cn("text-[10px] font-semibold px-2 py-0.5 rounded-full shrink-0", c.badgeTone)}>
                {c.badge}
              </span>
            </div>
            <div className="text-[10px] font-semibold text-muted-foreground uppercase tracking-wide mt-3">
              {c.category}
            </div>
            <h3 className="mt-1 font-semibold leading-snug">{c.title}</h3>
            <p className="mt-1.5 text-sm text-muted-foreground leading-snug">{c.description}</p>
            <div className="mt-3 text-[12px] font-semibold text-primary">{c.footer}</div>
          </div>
        );
      })}
    </div>
  );
}

export default function StoriesPage() {
  const [category, setCategory] = useState<Category>("All");
  const { data: customerStories, isLoading: isLoadingCustomerStories } = useCustomerStories();
  const { data: testimonials, isLoading: isLoadingTestimonials } = useTestimonials();
  const { data: documentCustomerStories, isLoading: isLoadingDocumentCustomerStories } =
    useDocumentCustomerStories();
  const { data: documentTestimonials, isLoading: isLoadingDocumentTestimonials } =
    useDocumentTestimonials();

  const cards = useMemo(
    () => buildCards(customerStories ?? [], testimonials ?? []),
    [customerStories, testimonials]
  );
  const filtered = useMemo(
    () => (category === "All" ? cards : cards.filter((c) => c.category === category)),
    [cards, category]
  );
  const isLoading =
    (category === "All" && (isLoadingCustomerStories || isLoadingTestimonials))
    || (category === "Customer Zero Story" && isLoadingCustomerStories)
    || (category === "Testimonial" && isLoadingTestimonials);

  const documentCards = useMemo(
    () => buildDocumentCards(documentCustomerStories ?? [], documentTestimonials ?? []),
    [documentCustomerStories, documentTestimonials]
  );
  const filteredDocumentCards = useMemo(
    () =>
      category === "All"
        ? documentCards
        : documentCards.filter((c) => c.category === category),
    [documentCards, category]
  );
  const isLoadingDocumentCards =
    (category === "All" && (isLoadingDocumentCustomerStories || isLoadingDocumentTestimonials))
    || (category === "Customer Zero Story" && isLoadingDocumentCustomerStories)
    || (category === "Testimonial" && isLoadingDocumentTestimonials);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Proof"
        title="Stories & Evidence"
        description="A unified library for Customer Zero stories and testimonials — proof points ready to become case studies, briefs, or executive quotes."
        actions={
          <Button className="rounded-xl bg-copilot-gradient text-white">
            <Sparkles className="size-4" /> Auto-draft from evidence
          </Button>
        }
      />

      <div className="glass rounded-2xl p-1.5 flex flex-wrap gap-1">
        {categories.map((c) => (
          <button
            key={c}
            onClick={() => setCategory(c)}
            className={cn(
              "text-[13px] font-medium px-3.5 py-1.5 rounded-xl transition-colors",
              category === c
                ? "bg-copilot-gradient text-white shadow-sm"
                : "text-muted-foreground hover:text-foreground hover:bg-white/60"
            )}
          >
            {c}
          </button>
        ))}
      </div>

      <StoryCardGrid
        cards={filtered}
        isLoading={isLoading}
        emptyTitle="No stories yet"
        emptyDescription="Submit a Customer Story or Testimonial contribution against an Initiative and it will show up here."
      />

      <div className="pt-2 space-y-1">
        <h2 className="text-base font-semibold">Extracted from documents</h2>
        <p className="text-sm text-muted-foreground">
          Testimonials and customer stories automatically found in contribution attachments.
        </p>
      </div>

      <StoryCardGrid
        cards={filteredDocumentCards}
        isLoading={isLoadingDocumentCards}
        emptyTitle="Nothing extracted yet"
        emptyDescription="When a contribution's attached documents contain a testimonial or customer story, it will show up here automatically."
        documentSourced
      />
    </div>
  );
}
