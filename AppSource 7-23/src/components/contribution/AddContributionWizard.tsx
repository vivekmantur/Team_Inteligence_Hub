import { useEffect, useMemo, useRef, useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { useInitiativesQuery } from "@/hooks/use-initiatives-api";
import { useUsers } from "@/hooks/use-users";
import { useBackendUser } from "@/hooks/use-backend-user";
import {
  LINK_SOURCE_OPTIONS,
  REUSE_TARGET_OPTIONS,
  linkSourceToWire,
  reuseTargetToWire,
  useCreateContribution,
  useUploadContributionAttachment,
  type ContributionLinkSourceWire,
  type ContributionPriorityWire,
  type ContributionStatusWire,
  type ContributionTypeWire,
  type RiskSeverityWire,
  type SaveContributionRequest,
} from "@/hooks/use-contributions";
import {
  ArrowLeft,
  ArrowRight,
  Check,
  CheckCircle2,
  ChevronRight,
  FileText,
  FileSpreadsheet,
  FileType,
  Film,
  Image as ImageIcon,
  Link as LinkIcon,
  Paperclip,
  Plus,
  Presentation,
  Save,
  Sparkles,
  Target,
  Trash2,
  Upload,
  User,
  X,
  BarChart3,
  AlertTriangle,
  Lightbulb,
  Heart,
  MessagesSquare,
  BookOpenCheck,
  HandHelping,
  CheckSquare,
  MoreHorizontal,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface Props {
  open: boolean;
  onOpenChange: (o: boolean) => void;
  preselectedInitiativeId?: string;
}

/**
 * `id` is the card's identity in this component; `wire` is the enum member the API
 * stores. Keeping both here means the mapping lives in one place instead of a switch
 * somewhere down in the submit handler.
 */
const CONTRIBUTION_TYPES: {
  id: string;
  wire: ContributionTypeWire;
  label: string;
  icon: typeof CheckSquare;
  gradient: string;
  desc: string;
}[] = [
  { id: "progress", wire: "ProgressUpdate", label: "Progress Update", icon: CheckSquare, gradient: "from-indigo-500 to-blue-500", desc: "Share status on an active workstream" },
  { id: "deliverable", wire: "Deliverable", label: "Deliverable", icon: BookOpenCheck, gradient: "from-sky-500 to-cyan-500", desc: "Ship a doc, deck, workshop, or campaign" },
  { id: "customer_story", wire: "CustomerStory", label: "Customer Story", icon: Heart, gradient: "from-rose-500 to-orange-500", desc: "Capture a real customer outcome" },
  { id: "metric", wire: "BusinessMetric", label: "Business Metric", icon: BarChart3, gradient: "from-emerald-500 to-teal-500", desc: "Log a measurable business result" },
  { id: "risk", wire: "Risk", label: "Risk / Blocker", icon: AlertTriangle, gradient: "from-amber-500 to-rose-500", desc: "Flag something that needs attention" },
  { id: "decision", wire: "Decision", label: "Decision", icon: CheckCircle2, gradient: "from-violet-500 to-fuchsia-500", desc: "Record a key decision and rationale" },
  { id: "ai_practice", wire: "AiBestPractice", label: "AI Best Practice", icon: Lightbulb, gradient: "from-fuchsia-500 to-purple-500", desc: "Share a repeatable AI/Copilot pattern" },
  { id: "testimonial", wire: "Testimonial", label: "Testimonial", icon: MessagesSquare, gradient: "from-pink-500 to-rose-500", desc: "Quote from a stakeholder or customer" },
  { id: "asset", wire: "SupportingAsset", label: "Supporting Asset", icon: Paperclip, gradient: "from-slate-500 to-slate-700", desc: "Upload a file or link" },
  { id: "support", wire: "SupportNeeded", label: "Support Needed", icon: HandHelping, gradient: "from-orange-500 to-red-500", desc: "Ask for help or unblock a request" },
  { id: "other", wire: "Other", label: "Other", icon: MoreHorizontal, gradient: "from-neutral-400 to-neutral-600", desc: "Something else worth capturing" },
];

/** Labels come from the hook so the UI and the wire format cannot drift apart. */
const REUSE_TARGETS = REUSE_TARGET_OPTIONS.map((option) => option.label);

const LINK_SOURCES = LINK_SOURCE_OPTIONS.map((option) => option.label);

/** A file chosen but not yet uploaded. The upload needs a ContributionId first. */
type PendingFile = { id: string; file: File };

type RiskDraft = {
  description: string;
  severity: RiskSeverityWire;
  businessImpact: string;
  mitigation: string;
  supportNeeded: string;
  ownerUserId: number | null;
  targetResolutionDate: string;
};

// Blank values live here rather than inline in useState so that opening the dialog can
// reset to exactly what it started with. The wizard is mounted for the life of the app
// and only toggles `open`, so nothing is cleared for us between uses.
const EMPTY_METRIC = {
  metricName: "",
  previousValue: "",
  currentValue: "",
  unit: "",
  reportingPeriod: "Q3 FY26",
};

const EMPTY_RISK: RiskDraft = {
  description: "",
  severity: "Medium",
  businessImpact: "",
  mitigation: "",
  supportNeeded: "",
  ownerUserId: null,
  targetResolutionDate: "",
};

const EMPTY_AI = { tool: "Copilot", useCase: "", prompt: "", timeSaved: "", recommendation: "" };

const EMPTY_STORY = { customer: "", summary: "", outcome: "", quote: "", businessValue: "" };

const EMPTY_ASSET = { description: "", link: "" };

/** Reads a numeric input, treating blank as "not supplied" rather than zero. */
function toNumber(value: string): number | null {
  const trimmed = value.trim();

  if (trimmed === "") return null;

  const parsed = Number(trimmed);

  return Number.isFinite(parsed) ? parsed : null;
}

function blankToNull(value: string): string | null {
  const trimmed = value.trim();

  return trimmed === "" ? null : trimmed;
}

/** The submitter's own row in the contributors list. */
function creditFor(user: { id: number; displayName: string; email: string }) {
  return {
    id: String(user.id),
    name: user.displayName,
    role: user.email,
    area: "Submitter",
    primary: true,
  };
}

const STEPS = [
  { n: 1, label: "Initiative" },
  { n: 2, label: "Type" },
  { n: 3, label: "Summary" },
  { n: 4, label: "Details" },
  { n: 5, label: "Evidence" },
  { n: 6, label: "Contributors" },
  { n: 7, label: "Visibility" },
  { n: 8, label: "Review" },
];

function fileIconFor(name: string) {
  const ext = name.split(".").pop()?.toLowerCase() ?? "";
  if (["ppt", "pptx"].includes(ext)) return Presentation;
  if (["doc", "docx"].includes(ext)) return FileType;
  if (["xls", "xlsx", "csv"].includes(ext)) return FileSpreadsheet;
  if (["pdf"].includes(ext)) return FileText;
  if (["png", "jpg", "jpeg", "gif", "webp", "svg"].includes(ext)) return ImageIcon;
  if (["mp4", "mov", "webm", "avi"].includes(ext)) return Film;
  return FileText;
}

function humanSize(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function AddContributionWizard({ open, onOpenChange, preselectedInitiativeId }: Props) {
  const [step, setStep] = useState(1);
  const [submitted, setSubmitted] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  // Step 1
  const [initiativeId, setInitiativeId] = useState<string>("");
  const [initiativeQuery, setInitiativeQuery] = useState("");

  // Step 2
  const [types, setTypes] = useState<string[]>([]);

  // Step 3
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [keyTakeaway, setKeyTakeaway] = useState("");
  const [priority, setPriority] = useState<ContributionPriorityWire>("Medium");
  const [tagInput, setTagInput] = useState("");
  const [tags, setTags] = useState<string[]>([]);

  // Step 4 dynamic. Numbers are held as strings because that is what an <input> gives
  // back; they are parsed once, on submit.
  const [metric, setMetric] = useState(EMPTY_METRIC);
  const [risk, setRisk] = useState<RiskDraft>(EMPTY_RISK);
  const [ai, setAi] = useState(EMPTY_AI);
  const [story, setStory] = useState(EMPTY_STORY);
  const [asset, setAsset] = useState(EMPTY_ASSET);

  // Step 5
  const [files, setFiles] = useState<PendingFile[]>([]);
  const [links, setLinks] = useState<{ id: string; source: string; url: string; label: string }[]>([]);
  const [linkSource, setLinkSource] = useState(LINK_SOURCES[0]);
  const [linkUrl, setLinkUrl] = useState("");
  const [linkLabel, setLinkLabel] = useState("");
  const [dragOver, setDragOver] = useState(false);

  // Step 6
  const [contributors, setContributors] = useState<{ id: string; name: string; role: string; area: string; primary: boolean }[]>([]);
  const [personQuery, setPersonQuery] = useState("");

  // Step 7
  const [visibility, setVisibility] = useState<string[]>([]);

  const fileInputRef = useRef<HTMLInputElement>(null);

  // Real data. Initiatives and people come from the API, and the submitter is whoever
  // is signed in rather than a constant.
  const { data: backendUser } = useBackendUser();
  const { data: apiInitiatives = [] } = useInitiativesQuery();
  const { data: apiUsers = [] } = useUsers();

  const submitter = useMemo(
    () => ({
      id: backendUser ? String(backendUser.id) : "me",
      name: backendUser?.displayName ?? "You",
      role: backendUser?.email ?? "",
    }),
    [backendUser],
  );

  /**
   * The Initiative cards want a flat shape. Progress is not a column the API has, so it
   * reads zero rather than being invented here.
   */
  const projects = useMemo(
    () =>
      apiInitiatives.map((item) => ({
        id: String(item.id),
        name: item.name,
        workstream: item.businessArea,
        owner: item.ownerDisplayName ?? "Unassigned",
        progress: 0,
      })),
    [apiInitiatives],
  );

  const people = useMemo(
    () =>
      apiUsers
        .filter((user) => user.isActive)
        .map((user) => ({
          id: String(user.id),
          name: user.displayName,
          subtitle: user.email,
        })),
    [apiUsers],
  );

  const numericInitiativeId = /^\d+$/.test(initiativeId) ? Number(initiativeId) : null;

  const createContribution = useCreateContribution(numericInitiativeId);
  const uploadAttachment = useUploadContributionAttachment(numericInitiativeId);

  const initiative = useMemo(() => projects.find((p) => p.id === initiativeId), [projects, initiativeId]);

  /**
   * Clears every field when the dialog opens.
   *
   * The wizard stays mounted for the life of the app and only toggles `open`, so React
   * keeps its state between uses. Without this, submitting a contribution and reopening
   * showed the previous one's answers, one Next away from being submitted again.
   */
  useEffect(() => {
    if (!open) return;

    setStep(1);
    setSubmitted(false);
    setSaveError(null);
    setSaving(false);

    // Step 1. Blank unless the caller named an Initiative, e.g. the button on an
    // Initiative's own page.
    setInitiativeId(preselectedInitiativeId ?? "");
    setInitiativeQuery("");

    setTypes([]);

    setTitle("");
    setDescription("");
    setKeyTakeaway("");
    setPriority("Medium");
    setTagInput("");
    setTags([]);

    setMetric(EMPTY_METRIC);
    setRisk(EMPTY_RISK);
    setAi(EMPTY_AI);
    setStory(EMPTY_STORY);
    setAsset(EMPTY_ASSET);

    setFiles([]);
    setLinks([]);
    setLinkSource(LINK_SOURCES[0]);
    setLinkUrl("");
    setLinkLabel("");
    setDragOver(false);

    // Seeded here rather than left to the effect below, which only fires when the
    // profile itself changes and would not run again on a later open.
    setContributors(
      backendUser
        ? [creditFor(backendUser)]
        : [],
    );
    setPersonQuery("");

    setVisibility([]);
    // backendUser is deliberately not a dependency: this runs on open, and the effect
    // below covers a profile that resolves while the dialog is already up.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, preselectedInitiativeId]);

  // Covers the profile resolving after the dialog is already open. The submitter is
  // always credited, which mirrors what the API does server-side anyway.
  useEffect(() => {
    if (!backendUser) return;

    setContributors((prev) =>
      prev.some((c) => c.id === String(backendUser.id))
        ? prev
        : [creditFor(backendUser), ...prev],
    );
  }, [backendUser]);

  const filteredInitiatives = useMemo(() => {
    const q = initiativeQuery.toLowerCase();
    if (!q) return projects;
    return projects.filter((p) => p.name.toLowerCase().includes(q) || p.workstream.toLowerCase().includes(q));
  }, [projects, initiativeQuery]);

  const filteredPeople = useMemo(() => {
    const q = personQuery.toLowerCase();
    return people.filter(
      (m) =>
        !contributors.some((c) => c.id === m.id) &&
        (q === "" || m.name.toLowerCase().includes(q) || m.subtitle.toLowerCase().includes(q))
    );
  }, [people, personQuery, contributors]);

  const stepFields: Record<number, string[]> = {
    1: initiativeId ? ["ok"] : [],
    2: types,
    3: [title, description].filter(Boolean),
    4: ["ok"],
    5: ["ok"],
    6: contributors.map((c) => c.id),
    7: ["ok"],
    8: ["ok"],
  };

  const totalRequired = 8;
  const completedSteps = Object.entries(stepFields).filter(([, v]) => v.length > 0).length;
  const completionPct = Math.round((completedSteps / totalRequired) * 100);

  const canNext =
    (step === 1 && !!initiativeId) ||
    (step === 2 && types.length > 0) ||
    (step === 3 && title.trim() !== "" && description.trim() !== "") ||
    (step >= 4 && step < 8);

  const showMetric = types.includes("metric");
  const showRisk = types.includes("risk");
  const showAI = types.includes("ai_practice");
  const showStory = types.includes("customer_story");
  const showAsset = types.includes("asset");
  const hasStep4Section = showMetric || showRisk || showAI || showStory || showAsset;

  function toggleType(id: string) {
    setTypes((prev) => (prev.includes(id) ? prev.filter((t) => t !== id) : [...prev, id]));
  }

  function addTag() {
    const t = tagInput.trim();
    if (!t) return;
    if (!tags.includes(t)) setTags([...tags, t]);
    setTagInput("");
  }

  function handleFiles(list: FileList | File[]) {
    // Read the list before anything resets the input. A state updater runs lazily, so
    // reading it inside one would see an already-cleared list.
    const picked = Array.from(list);
    if (picked.length === 0) return;
    const stamp = Date.now();
    setFiles((prev) => [
      ...prev,
      ...picked.map((file, i) => ({ id: `${stamp}-${i}-${file.name}`, file })),
    ]);
  }

  function addLink() {
    if (!linkUrl.trim()) return;
    setLinks((prev) => [
      ...prev,
      { id: `${Date.now()}`, source: linkSource, url: linkUrl.trim(), label: linkLabel.trim() || linkUrl.trim() },
    ]);
    setLinkUrl("");
    setLinkLabel("");
  }

  function addContributor(id: string) {
    const person = people.find((p) => p.id === id);
    if (!person) return;
    setContributors((prev) => [
      ...prev,
      { id: person.id, name: person.name, role: person.subtitle, area: "", primary: false },
    ]);
    setPersonQuery("");
  }

  function removeContributor(id: string) {
    if (id === submitter.id) return;
    setContributors((prev) => prev.filter((c) => c.id !== id));
  }

  function togglePrimary(id: string) {
    setContributors((prev) => prev.map((c) => (c.id === id ? { ...c, primary: !c.primary } : c)));
  }

  function toggleVisibility(v: string) {
    setVisibility((prev) => (prev.includes(v) ? prev.filter((x) => x !== v) : [...prev, v]));
  }

  function goNext() {
    if (step < 8) setStep(step + 1);
  }

  function goBack() {
    if (step > 1) setStep(step - 1);
  }

  /**
   * Turns wizard state into the API payload.
   *
   * Detail sections are sent only when their type is selected: the API rejects a metric
   * on a Progress Update, which is the invariant the separate tables exist to protect.
   * The Supporting Asset section becomes a link, because that is where the backend
   * keeps URLs.
   */
  function buildRequest(status: ContributionStatusWire): SaveContributionRequest {
    const allLinks = links.map((l) => ({
      source: linkSourceToWire(l.source) as ContributionLinkSourceWire,
      url: l.url,
      label: blankToNull(l.label),
      description: null as string | null,
    }));

    if (showAsset && asset.link.trim() !== "") {
      allLinks.push({
        source: "ExternalUrl",
        url: asset.link.trim(),
        label: null,
        description: blankToNull(asset.description),
      });
    }

    return {
      title: title.trim(),
      description: description.trim(),
      keyTakeaway: blankToNull(keyTakeaway),
      priority,
      status,
      types: types
        .map((id) => CONTRIBUTION_TYPES.find((t) => t.id === id)?.wire)
        .filter((wire): wire is ContributionTypeWire => Boolean(wire)),
      tags,
      reuseTargets: visibility
        .map((label) => reuseTargetToWire(label))
        .filter((wire): wire is NonNullable<typeof wire> => Boolean(wire)),
      contributors: contributors
        .filter((c) => /^\d+$/.test(c.id))
        .map((c) => ({
          userId: Number(c.id),
          responsibilityArea: blankToNull(c.area),
          isPrimary: c.primary,
        })),
      links: allLinks,
      metric: showMetric
        ? {
            metricName: metric.metricName.trim(),
            unit: blankToNull(metric.unit),
            previousValue: toNumber(metric.previousValue),
            currentValue: toNumber(metric.currentValue),
            reportingPeriod: blankToNull(metric.reportingPeriod),
          }
        : null,
      risk: showRisk
        ? {
            description: risk.description.trim(),
            severity: risk.severity,
            businessImpact: blankToNull(risk.businessImpact),
            mitigation: blankToNull(risk.mitigation),
            supportNeeded: blankToNull(risk.supportNeeded),
            ownerUserId: risk.ownerUserId,
            targetResolutionDate: blankToNull(risk.targetResolutionDate),
          }
        : null,
      aiPractice: showAI
        ? {
            tool: ai.tool.trim(),
            useCase: blankToNull(ai.useCase),
            prompt: blankToNull(ai.prompt),
            timeSavedHoursPerWeek: toNumber(ai.timeSaved),
            recommendation: blankToNull(ai.recommendation),
          }
        : null,
      customerStory: showStory
        ? {
            customerName: story.customer.trim(),
            summary: blankToNull(story.summary),
            outcome: blankToNull(story.outcome),
            quote: blankToNull(story.quote),
            businessValue: blankToNull(story.businessValue),
          }
        : null,
    };
  }

  /**
   * Creates the contribution, then uploads each file against it.
   *
   * Order matters: an attachment row hangs off a ContributionId, so the contribution has
   * to exist first. A file that fails to upload does not roll back the contribution —
   * the text is the valuable part, and the person can retry the attachment.
   */
  async function save(status: ContributionStatusWire) {
    if (!numericInitiativeId) {
      setSaveError("Pick an Initiative first.");
      return null;
    }

    setSaving(true);
    setSaveError(null);

    try {
      const created = await createContribution.mutateAsync(buildRequest(status));

      const failed: string[] = [];

      for (const pending of files) {
        try {
          await uploadAttachment.mutateAsync({ contributionId: created.id, file: pending.file });
        } catch {
          failed.push(pending.file.name);
        }
      }

      if (failed.length > 0) {
        setSaveError(
          `Saved, but these files did not upload: ${failed.join(", ")}. Open the contribution to try again.`,
        );
      }

      return created;
    } catch (error) {
      setSaveError(error instanceof Error ? error.message : "Could not save the contribution.");
      return null;
    } finally {
      setSaving(false);
    }
  }

  async function handleSaveDraft() {
    const created = await save("Draft");
    if (created) onOpenChange(false);
  }

  async function handleSubmit() {
    const created = await save("Submitted");
    if (!created) return;
    setSubmitted(true);
    setTimeout(() => onOpenChange(false), 1600);
  }

  const summaryTypes = types
    .map((t) => CONTRIBUTION_TYPES.find((c) => c.id === t)?.label ?? t)
    .filter(Boolean);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        showCloseButton={false}
        className="p-0 w-[min(1200px,96vw)] max-w-[min(1200px,96vw)] sm:max-w-[min(1200px,96vw)] grid-rows-[minmax(0,1fr)] h-[min(86vh,900px)] max-h-[calc(100vh-3rem)] rounded-2xl overflow-hidden border-white/60 bg-gradient-to-br from-white via-white to-indigo-50/40"
      >
        <DialogTitle className="sr-only">Add Contribution</DialogTitle>

        {submitted ? (
          <SuccessView initiativeName={initiative?.name ?? ""} onClose={() => onOpenChange(false)} />
        ) : (
          <div className="flex flex-col h-full">
            {/* Header */}
            <div className="px-5 md:px-7 pt-5 pb-4 border-b border-black/5 bg-white/60 backdrop-blur">
              <div className="flex items-start gap-3">
                <div className="size-10 rounded-xl bg-copilot-gradient grid place-items-center text-white shadow-md shrink-0">
                  <Sparkles className="size-5" />
                </div>
                <div className="flex-1 min-w-0">
                  <div className="text-[11px] font-semibold uppercase tracking-[0.14em] text-gradient">
                    Add Contribution
                  </div>
                  <h2 className="text-lg md:text-xl font-semibold leading-tight">
                    Capture something valuable for your Initiative
                  </h2>
                </div>
                <button
                  onClick={() => onOpenChange(false)}
                  className="size-9 rounded-lg hover:bg-black/5 grid place-items-center text-muted-foreground"
                  aria-label="Close"
                >
                  <X className="size-4" />
                </button>
              </div>

              {/* Progress steps */}
              <div className="mt-4 -mx-1 overflow-x-auto no-scrollbar">
                <div className="flex items-center gap-1 min-w-max px-1">
                  {STEPS.map((s, i) => {
                    const active = step === s.n;
                    const done = step > s.n;
                    return (
                      <div key={s.n} className="flex items-center gap-1">
                        <button
                          onClick={() => setStep(s.n)}
                          className={cn(
                            "flex items-center gap-2 rounded-full px-3 h-8 text-[12px] font-medium transition-all",
                            active && "bg-copilot-gradient text-white shadow-sm",
                            !active && done && "bg-emerald-50 text-emerald-700",
                            !active && !done && "bg-white/70 text-muted-foreground hover:text-foreground"
                          )}
                        >
                          <span
                            className={cn(
                              "grid place-items-center size-5 rounded-full text-[10px] font-bold",
                              active ? "bg-white/20 text-white" : done ? "bg-emerald-500 text-white" : "bg-muted text-muted-foreground"
                            )}
                          >
                            {done ? <Check className="size-3" /> : s.n}
                          </span>
                          <span className="hidden sm:inline">{s.label}</span>
                        </button>
                        {i < STEPS.length - 1 && <ChevronRight className="size-3 text-muted-foreground/60" />}
                      </div>
                    );
                  })}
                </div>
              </div>
            </div>

            {/* Body */}
            <div className="flex-1 min-h-0 grid grid-rows-[minmax(0,1fr)] md:grid-cols-[1fr_320px] overflow-hidden">
              <div className="overflow-y-auto px-5 md:px-7 py-6 min-h-0">
                {step === 1 && (
                  <Step1
                    initiativeId={initiativeId}
                    setInitiativeId={setInitiativeId}
                    query={initiativeQuery}
                    setQuery={setInitiativeQuery}
                    items={filteredInitiatives}
                    submitter={submitter}
                    initiative={initiative}
                  />
                )}
                {step === 2 && <Step2 types={types} toggleType={toggleType} />}
                {step === 3 && (
                  <Step3
                    title={title}
                    setTitle={setTitle}
                    description={description}
                    setDescription={setDescription}
                    keyTakeaway={keyTakeaway}
                    setKeyTakeaway={setKeyTakeaway}
                    priority={priority}
                    setPriority={setPriority}
                    tags={tags}
                    setTags={setTags}
                    tagInput={tagInput}
                    setTagInput={setTagInput}
                    onAddTag={addTag}
                  />
                )}
                {step === 4 && (
                  <Step4
                    hasAny={hasStep4Section}
                    showMetric={showMetric}
                    showRisk={showRisk}
                    showAI={showAI}
                    showStory={showStory}
                    showAsset={showAsset}
                    metric={metric}
                    setMetric={setMetric}
                    risk={risk}
                    setRisk={setRisk}
                    ai={ai}
                    setAi={setAi}
                    story={story}
                    setStory={setStory}
                    asset={asset}
                    setAsset={setAsset}
                    types={types}
                    people={people}
                  />
                )}
                {step === 5 && (
                  <Step5
                    files={files}
                    setFiles={setFiles}
                    links={links}
                    onAddLink={addLink}
                    linkSource={linkSource}
                    setLinkSource={setLinkSource}
                    linkUrl={linkUrl}
                    setLinkUrl={setLinkUrl}
                    linkLabel={linkLabel}
                    setLinkLabel={setLinkLabel}
                    dragOver={dragOver}
                    setDragOver={setDragOver}
                    handleFiles={handleFiles}
                    fileInputRef={fileInputRef}
                    onRemoveLink={(id: string) => setLinks((prev) => prev.filter((l) => l.id !== id))}
                    onRemoveFile={(id: string) => setFiles((prev) => prev.filter((f) => f.id !== id))}
                  />
                )}
                {step === 6 && (
                  <Step6
                    contributors={contributors}
                    setContributors={setContributors}
                    filteredPeople={filteredPeople}
                    personQuery={personQuery}
                    setPersonQuery={setPersonQuery}
                    onAdd={addContributor}
                    onRemove={removeContributor}
                    onTogglePrimary={togglePrimary}
                  />
                )}
                {step === 7 && <Step7 visibility={visibility} toggle={toggleVisibility} />}
                {step === 8 && (
                  <Step8
                    initiativeName={initiative?.name}
                    workstream={initiative?.workstream}
                    types={summaryTypes}
                    title={title}
                    description={description}
                    keyTakeaway={keyTakeaway}
                    priority={priority}
                    tags={tags}
                    files={files}
                    links={links}
                    contributors={contributors}
                    visibility={visibility}
                    onJumpTo={setStep}
                  />
                )}
              </div>

              {/* Right summary panel (desktop) */}
              <aside className="hidden md:block border-l border-black/5 bg-white/50 backdrop-blur overflow-y-auto">
                <div className="p-5 space-y-5">
                  <div>
                    <div className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">Summary</div>
                    <div className="mt-2 text-[11px] text-muted-foreground">Completion</div>
                    <div className="mt-1 h-1.5 rounded-full bg-muted overflow-hidden">
                      <div className="h-full bg-copilot-gradient transition-all" style={{ width: `${completionPct}%` }} />
                    </div>
                    <div className="mt-1 text-[11px] text-muted-foreground">{completionPct}% complete</div>
                  </div>

                  <SummaryRow label="Initiative" icon={<Target className="size-3.5" />}>
                    {initiative ? (
                      <div>
                        <div className="text-[13px] font-medium leading-tight">{initiative.name}</div>
                        <div className="text-[11px] text-muted-foreground">{initiative.workstream}</div>
                      </div>
                    ) : (
                      <span className="text-[12px] text-muted-foreground">Not selected</span>
                    )}
                  </SummaryRow>

                  <SummaryRow label="Contribution type" icon={<Sparkles className="size-3.5" />}>
                    {summaryTypes.length === 0 ? (
                      <span className="text-[12px] text-muted-foreground">Pick one or more</span>
                    ) : (
                      <div className="flex flex-wrap gap-1">
                        {summaryTypes.map((t) => (
                          <span key={t} className="text-[10px] font-medium px-2 py-0.5 rounded-full bg-copilot-gradient text-white">
                            {t}
                          </span>
                        ))}
                      </div>
                    )}
                  </SummaryRow>

                  <SummaryRow label="Files attached" icon={<Paperclip className="size-3.5" />}>
                    <div className="text-[13px] font-medium">
                      {files.length + links.length} item{files.length + links.length === 1 ? "" : "s"}
                    </div>
                    <div className="text-[11px] text-muted-foreground">
                      {files.length} file{files.length === 1 ? "" : "s"} · {links.length} link{links.length === 1 ? "" : "s"}
                    </div>
                  </SummaryRow>

                  <SummaryRow label="Contributors" icon={<User className="size-3.5" />}>
                    <div className="flex -space-x-2">
                      {contributors.slice(0, 5).map((c, i) => (
                        <div
                          key={c.id}
                          className={cn(
                            "size-7 rounded-full bg-gradient-to-br grid place-items-center text-[10px] font-semibold text-white ring-2 ring-white",
                            ["from-indigo-500 to-fuchsia-500", "from-sky-500 to-cyan-500", "from-rose-500 to-orange-500", "from-emerald-500 to-teal-500", "from-amber-500 to-rose-500"][i % 5]
                          )}
                        >
                          {c.name.split(" ").map((n) => n[0]).join("").slice(0, 2)}
                        </div>
                      ))}
                      {contributors.length > 5 && (
                        <div className="size-7 rounded-full bg-muted grid place-items-center text-[10px] font-semibold ring-2 ring-white">
                          +{contributors.length - 5}
                        </div>
                      )}
                    </div>
                  </SummaryRow>

                  <div className="rounded-xl bg-white/70 p-3 ring-gradient">
                    <div className="flex items-center gap-2 text-[11px] font-semibold text-gradient">
                      <Sparkles className="size-3" /> AI tip
                    </div>
                    <p className="mt-1 text-[11px] leading-relaxed text-muted-foreground">
                      Great contributions are specific. Add a metric or a customer quote to make this reusable in QBRs and Executive Briefs.
                    </p>
                  </div>
                </div>
              </aside>
            </div>

            {/* Sticky footer */}
            <div className="border-t border-black/5 bg-white/80 backdrop-blur px-5 md:px-7 py-3 flex items-center gap-2">
              <Button variant="ghost" onClick={goBack} disabled={step === 1} className="rounded-xl">
                <ArrowLeft className="size-4" />
                Back
              </Button>
              <div className="text-[11px] text-muted-foreground hidden sm:block">
                Step {step} of {STEPS.length}
              </div>
              <div className="ml-auto flex items-center gap-2">
                <Button variant="outline" onClick={handleSaveDraft} className="rounded-xl bg-white" disabled={!initiativeId || saving}>
                  <Save className="size-4" />
                  Save draft
                </Button>
                {step < 8 ? (
                  <Button onClick={goNext} disabled={!canNext} className="rounded-xl bg-copilot-gradient text-white shadow-md">
                    Next
                    <ArrowRight className="size-4" />
                  </Button>
                ) : (
                  <Button onClick={handleSubmit} disabled={!initiativeId || !title.trim() || saving} className="rounded-xl bg-copilot-gradient text-white shadow-md">
                    <Sparkles className="size-4" />
                    Submit contribution
                  </Button>
                )}
              </div>
            </div>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}

/* ---------------------- Sub-components ---------------------- */

function SummaryRow({ label, icon, children }: { label: string; icon: React.ReactNode; children: React.ReactNode }) {
  return (
    <div>
      <div className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">
        {icon}
        {label}
      </div>
      <div className="mt-1.5">{children}</div>
    </div>
  );
}

function SectionTitle({ eyebrow, title, description }: { eyebrow: string; title: string; description: string }) {
  return (
    <div className="mb-5">
      <div className="text-[11px] font-semibold uppercase tracking-[0.14em] text-gradient">{eyebrow}</div>
      <h3 className="mt-1 text-xl font-semibold tracking-tight">{title}</h3>
      <p className="text-sm text-muted-foreground mt-1">{description}</p>
    </div>
  );
}

/* Step 1 */
function Step1({ initiativeId, setInitiativeId, query, setQuery, items, submitter, initiative }: any) {
  return (
    <div>
      <SectionTitle
        eyebrow="Step 1"
        title="Initiative Context"
        description="Which Initiative does this contribution belong to?"
      />
      <div className="grid md:grid-cols-2 gap-3 mb-5">
        <div className="rounded-xl bg-white/80 border border-black/5 p-3">
          <div className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">Submitted by</div>
          <div className="mt-1 flex items-center gap-2">
            <div className="size-7 rounded-full bg-gradient-to-br from-indigo-500 to-fuchsia-500 grid place-items-center text-[10px] font-semibold text-white">
              {submitter.name.split(" ").map((n: string) => n[0]).join("").slice(0, 2)}
            </div>
            <div className="text-[13px] font-medium">{submitter.name}</div>
          </div>
        </div>
        <div className="rounded-xl bg-white/80 border border-black/5 p-3">
          <div className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">Submission date</div>
          <div className="mt-1 text-[13px] font-medium">
            {new Date().toLocaleDateString(undefined, { year: "numeric", month: "short", day: "numeric" })}
          </div>
        </div>
      </div>

      <Label className="text-[12px] font-semibold">Initiative <span className="text-rose-500">*</span></Label>
      <div className="relative mt-1.5">
        <Input
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Search initiatives by name or workstream…"
          className="h-10 bg-white rounded-xl"
        />
      </div>

      {/* -mx-1 + p-1: overflow-y-auto clips on every side, so the selected card's
          ring-2 and shadow need room inside the scroll box. The negative margin
          cancels the padding so the cards still line up with the search field. */}
      <div className="mt-2 -mx-1 grid sm:grid-cols-2 gap-2 max-h-[320px] overflow-y-auto p-1">
        {items.map((p: any) => {
          const selected = initiativeId === p.id;
          return (
            <button
              key={p.id}
              onClick={() => setInitiativeId(p.id)}
              className={cn(
                "text-left rounded-xl border p-3 transition-all bg-white",
                selected ? "border-transparent ring-2 ring-indigo-500 shadow-md" : "border-black/5 hover:border-indigo-200 hover:shadow-sm"
              )}
            >
              <div className="flex items-start justify-between gap-2">
                <div className="min-w-0">
                  <div className="text-[13px] font-semibold leading-tight truncate">{p.name}</div>
                  <div className="text-[11px] text-muted-foreground mt-0.5">{p.workstream} · {p.owner}</div>
                </div>
                {selected && (
                  <div className="size-6 rounded-full bg-copilot-gradient grid place-items-center text-white shrink-0">
                    <Check className="size-3.5" />
                  </div>
                )}
              </div>
              <div className="mt-2 h-1 rounded-full bg-muted overflow-hidden">
                <div className="h-full bg-copilot-gradient" style={{ width: `${p.progress}%` }} />
              </div>
            </button>
          );
        })}
      </div>

      {initiative && (
        <div className="mt-4 rounded-xl bg-white/80 border border-black/5 p-3">
          <div className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">Workstream (auto)</div>
          <div className="mt-1 text-[13px] font-medium">{initiative.workstream}</div>
        </div>
      )}
    </div>
  );
}

/* Step 2 */
function Step2({ types, toggleType }: { types: string[]; toggleType: (id: string) => void }) {
  return (
    <div>
      <SectionTitle
        eyebrow="Step 2"
        title="Contribution Type"
        description="Pick one or more. Only relevant sections will appear later."
      />
      <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-3">
        {CONTRIBUTION_TYPES.map((t) => {
          const Icon = t.icon;
          const active = types.includes(t.id);
          return (
            <button
              key={t.id}
              onClick={() => toggleType(t.id)}
              className={cn(
                "relative text-left rounded-2xl p-4 border transition-all bg-white overflow-hidden",
                active ? "border-transparent ring-2 ring-indigo-500 shadow-md -translate-y-0.5" : "border-black/5 hover:shadow-md hover:-translate-y-0.5"
              )}
            >
              <div className={cn("size-10 rounded-xl grid place-items-center text-white shadow bg-gradient-to-br", t.gradient)}>
                <Icon className="size-5" />
              </div>
              <div className="mt-3 text-[13px] font-semibold leading-tight">{t.label}</div>
              <div className="mt-1 text-[11px] text-muted-foreground leading-relaxed">{t.desc}</div>
              {active && (
                <div className="absolute top-3 right-3 size-6 rounded-full bg-copilot-gradient grid place-items-center text-white">
                  <Check className="size-3.5" />
                </div>
              )}
            </button>
          );
        })}
      </div>
    </div>
  );
}

/* Step 3 */
function Step3(props: any) {
  const { title, setTitle, description, setDescription, keyTakeaway, setKeyTakeaway, priority, setPriority, tags, setTags, tagInput, setTagInput, onAddTag } = props;
  return (
    <div>
      <SectionTitle eyebrow="Step 3" title="Contribution Summary" description="A short, high-signal write-up. Copilot uses this everywhere." />
      <div className="space-y-4">
        <div>
          <Label className="text-[12px] font-semibold">Title</Label>
          <Input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="e.g. Role Hub APAC pilot outperformed adoption target" className="h-10 bg-white rounded-xl mt-1.5" />
        </div>
        <div>
          <Label className="text-[12px] font-semibold">Description</Label>
          <Textarea value={description} onChange={(e) => setDescription(e.target.value)} placeholder="What happened, why it matters, and what changed…" className="bg-white rounded-xl mt-1.5 min-h-[110px]" />
        </div>
        <div>
          <Label className="text-[12px] font-semibold">Key takeaway</Label>
          <Input value={keyTakeaway} onChange={(e) => setKeyTakeaway(e.target.value)} placeholder="One sentence a leader could quote in a QBR" className="h-10 bg-white rounded-xl mt-1.5" />
        </div>
        <div className="grid sm:grid-cols-2 gap-4">
          <div>
            <Label className="text-[12px] font-semibold">Priority</Label>
            <div className="mt-1.5 flex gap-1.5">
              {["Low", "Medium", "High", "Critical"].map((p) => (
                <button
                  key={p}
                  onClick={() => setPriority(p)}
                  className={cn(
                    "h-10 px-3 rounded-xl text-[12px] font-medium border transition",
                    priority === p ? "bg-copilot-gradient text-white border-transparent shadow-sm" : "bg-white border-black/5 hover:border-indigo-200"
                  )}
                >
                  {p}
                </button>
              ))}
            </div>
          </div>
          <div>
            <Label className="text-[12px] font-semibold">Tags</Label>
            <div className="mt-1.5 flex gap-2">
              <Input
                value={tagInput}
                onChange={(e) => setTagInput(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    e.preventDefault();
                    onAddTag();
                  }
                }}
                placeholder="Type a tag and press Enter"
                className="h-10 bg-white rounded-xl"
              />
              <Button variant="outline" className="rounded-xl bg-white" onClick={onAddTag}>
                <Plus className="size-4" />
              </Button>
            </div>
            {tags.length > 0 && (
              <div className="mt-2 flex flex-wrap gap-1.5">
                {tags.map((t: string) => (
                  <span key={t} className="inline-flex items-center gap-1 text-[11px] px-2 py-0.5 rounded-full bg-white border border-black/5">
                    {t}
                    <button onClick={() => setTags(tags.filter((x: string) => x !== t))} className="opacity-60 hover:opacity-100">
                      <X className="size-3" />
                    </button>
                  </span>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

/* Step 4 */
function Step4(props: any) {
  const { hasAny, showMetric, showRisk, showAI, showStory, showAsset, metric, setMetric, risk, setRisk, ai, setAi, story, setStory, asset, setAsset, types, people } = props;

  if (!hasAny) {
    return (
      <div>
        <SectionTitle eyebrow="Step 4" title="Dynamic Sections" description="No extra details required for the types you selected." />
        <div className="rounded-2xl border border-dashed border-indigo-200 bg-white/60 p-8 text-center">
          <div className="mx-auto size-12 rounded-2xl bg-copilot-gradient grid place-items-center text-white shadow-md">
            <Sparkles className="size-5" />
          </div>
          <div className="mt-3 text-[14px] font-semibold">You're all set on this step</div>
          <p className="text-[12px] text-muted-foreground mt-1 max-w-md mx-auto">
            Based on your selected contribution type{types.length > 1 ? "s" : ""}, nothing extra is required here. Continue to attach evidence.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div>
      <SectionTitle eyebrow="Step 4" title="Dynamic Sections" description="Only the sections relevant to your contribution type are shown." />
      <div className="space-y-5">
        {showMetric && (
          <SectionCard title="Business Metric" icon={<BarChart3 className="size-4" />} gradient="from-emerald-500 to-teal-500">
            <div className="grid sm:grid-cols-2 gap-3">
              <Field label="Metric name"><Input value={metric.metricName} onChange={(e) => setMetric({ ...metric, metricName: e.target.value })} placeholder="e.g. Monthly active users" className="h-10 bg-white rounded-xl" /></Field>
              <Field label="Unit"><Input value={metric.unit} onChange={(e) => setMetric({ ...metric, unit: e.target.value })} placeholder="users / %" className="h-10 bg-white rounded-xl" /></Field>
              {/* Numeric so Analytics can compute the delta rather than only display it. */}
              <Field label="Previous value"><Input type="number" step="any" value={metric.previousValue} onChange={(e) => setMetric({ ...metric, previousValue: e.target.value })} placeholder="e.g. 1200" className="h-10 bg-white rounded-xl" /></Field>
              <Field label="Current value"><Input type="number" step="any" value={metric.currentValue} onChange={(e) => setMetric({ ...metric, currentValue: e.target.value })} placeholder="e.g. 1840" className="h-10 bg-white rounded-xl" /></Field>
              <Field label="Reporting period" full><Input value={metric.reportingPeriod} onChange={(e) => setMetric({ ...metric, reportingPeriod: e.target.value })} className="h-10 bg-white rounded-xl" /></Field>
            </div>
          </SectionCard>
        )}

        {showRisk && (
          <SectionCard title="Risk" icon={<AlertTriangle className="size-4" />} gradient="from-amber-500 to-rose-500">
            <div className="grid sm:grid-cols-2 gap-3">
              <Field label="Risk description" full><Textarea value={risk.description} onChange={(e) => setRisk({ ...risk, description: e.target.value })} className="bg-white rounded-xl min-h-[80px]" /></Field>
              <Field label="Severity">
                <div className="flex gap-1.5">
                  {["Low", "Medium", "High", "Critical"].map((s) => (
                    <button key={s} onClick={() => setRisk({ ...risk, severity: s })} className={cn("h-10 px-3 rounded-xl text-[12px] font-medium border", risk.severity === s ? "bg-amber-500 text-white border-transparent" : "bg-white border-black/5")}>
                      {s}
                    </button>
                  ))}
                </div>
              </Field>
              {/* A real person, not a typed-in name, so "risks I own" is a query. */}
              <Field label="Owner">
                <select
                  value={risk.ownerUserId ?? ""}
                  onChange={(e) => setRisk({ ...risk, ownerUserId: e.target.value === "" ? null : Number(e.target.value) })}
                  className="h-10 w-full rounded-xl border border-input bg-white px-3 text-[13px]"
                >
                  <option value="">Unassigned</option>
                  {(people as { id: string; name: string }[]).map((p) => (
                    <option key={p.id} value={p.id}>{p.name}</option>
                  ))}
                </select>
              </Field>
              <Field label="Business impact" full><Textarea value={risk.businessImpact} onChange={(e) => setRisk({ ...risk, businessImpact: e.target.value })} className="bg-white rounded-xl min-h-[70px]" /></Field>
              <Field label="Mitigation" full><Textarea value={risk.mitigation} onChange={(e) => setRisk({ ...risk, mitigation: e.target.value })} className="bg-white rounded-xl min-h-[70px]" /></Field>
              <Field label="Support needed"><Input value={risk.supportNeeded} onChange={(e) => setRisk({ ...risk, supportNeeded: e.target.value })} className="h-10 bg-white rounded-xl" /></Field>
              {/* A date rather than free text, so overdue risks can be found. */}
              <Field label="Target resolution"><Input type="date" value={risk.targetResolutionDate} onChange={(e) => setRisk({ ...risk, targetResolutionDate: e.target.value })} className="h-10 bg-white rounded-xl" /></Field>
            </div>
          </SectionCard>
        )}

        {showAI && (
          <SectionCard title="AI Best Practice" icon={<Lightbulb className="size-4" />} gradient="from-fuchsia-500 to-purple-500">
            <div className="grid sm:grid-cols-2 gap-3">
              <Field label="AI tool"><Input value={ai.tool} onChange={(e) => setAi({ ...ai, tool: e.target.value })} className="h-10 bg-white rounded-xl" /></Field>
              {/* Numeric so it can be summed across contributions, which is the point. */}
              <Field label="Time saved (hours per week)"><Input type="number" step="0.25" min="0" max="168" value={ai.timeSaved} onChange={(e) => setAi({ ...ai, timeSaved: e.target.value })} placeholder="e.g. 4" className="h-10 bg-white rounded-xl" /></Field>
              <Field label="Use case" full><Textarea value={ai.useCase} onChange={(e) => setAi({ ...ai, useCase: e.target.value })} className="bg-white rounded-xl min-h-[70px]" /></Field>
              <Field label="Prompt" full><Textarea value={ai.prompt} onChange={(e) => setAi({ ...ai, prompt: e.target.value })} placeholder="Paste the prompt so others can reuse it" className="bg-white rounded-xl min-h-[90px] font-mono text-[12px]" /></Field>
              <Field label="Recommendation" full><Textarea value={ai.recommendation} onChange={(e) => setAi({ ...ai, recommendation: e.target.value })} className="bg-white rounded-xl min-h-[70px]" /></Field>
            </div>
          </SectionCard>
        )}

        {showStory && (
          <SectionCard title="Customer Story" icon={<Heart className="size-4" />} gradient="from-rose-500 to-orange-500">
            <div className="grid sm:grid-cols-2 gap-3">
              <Field label="Customer"><Input value={story.customer} onChange={(e) => setStory({ ...story, customer: e.target.value })} className="h-10 bg-white rounded-xl" /></Field>
              <Field label="Outcome"><Input value={story.outcome} onChange={(e) => setStory({ ...story, outcome: e.target.value })} placeholder="e.g. 43% faster onboarding" className="h-10 bg-white rounded-xl" /></Field>
              <Field label="Story summary" full><Textarea value={story.summary} onChange={(e) => setStory({ ...story, summary: e.target.value })} className="bg-white rounded-xl min-h-[90px]" /></Field>
              <Field label="Quote" full><Textarea value={story.quote} onChange={(e) => setStory({ ...story, quote: e.target.value })} placeholder="“…”" className="bg-white rounded-xl min-h-[70px]" /></Field>
              <Field label="Business value" full><Input value={story.businessValue} onChange={(e) => setStory({ ...story, businessValue: e.target.value })} className="h-10 bg-white rounded-xl" /></Field>
            </div>
          </SectionCard>
        )}

        {showAsset && (
          <SectionCard title="Supporting Asset" icon={<Paperclip className="size-4" />} gradient="from-slate-500 to-slate-700">
            <div className="grid sm:grid-cols-2 gap-3">
              <Field label="Link" full><Input value={asset.link} onChange={(e) => setAsset({ ...asset, link: e.target.value })} placeholder="https://…" className="h-10 bg-white rounded-xl" /></Field>
              <Field label="Description" full><Textarea value={asset.description} onChange={(e) => setAsset({ ...asset, description: e.target.value })} className="bg-white rounded-xl min-h-[80px]" /></Field>
              <div className="sm:col-span-2 text-[11px] text-muted-foreground">
                You can also drag-and-drop files in the next step.
              </div>
            </div>
          </SectionCard>
        )}
      </div>
    </div>
  );
}

function SectionCard({ title, icon, gradient, children }: any) {
  return (
    <div className="rounded-2xl bg-white/90 border border-black/5 overflow-hidden">
      <div className="px-4 py-3 flex items-center gap-2 border-b border-black/5">
        <div className={cn("size-7 rounded-lg grid place-items-center text-white bg-gradient-to-br", gradient)}>{icon}</div>
        <div className="text-[13px] font-semibold">{title}</div>
      </div>
      <div className="p-4">{children}</div>
    </div>
  );
}

function Field({ label, children, full }: any) {
  return (
    <div className={cn(full && "sm:col-span-2")}>
      <Label className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider">{label}</Label>
      <div className="mt-1.5">{children}</div>
    </div>
  );
}

/* Step 5 */
function Step5(props: any) {
  const { files, links, onAddLink, linkSource, setLinkSource, linkUrl, setLinkUrl, linkLabel, setLinkLabel, dragOver, setDragOver, handleFiles, fileInputRef, onRemoveLink, onRemoveFile } = props;
  return (
    <div>
      <SectionTitle eyebrow="Step 5" title="Supporting Evidence" description="Attach docs, decks, videos, images, or link out to SharePoint, Teams, Loop, OneDrive." />

      <div
        onDragOver={(e) => {
          e.preventDefault();
          setDragOver(true);
        }}
        onDragLeave={() => setDragOver(false)}
        onDrop={(e) => {
          e.preventDefault();
          setDragOver(false);
          if (e.dataTransfer.files.length) handleFiles(e.dataTransfer.files);
        }}
        className={cn(
          "rounded-2xl border-2 border-dashed p-6 md:p-8 text-center transition-all",
          dragOver ? "border-indigo-500 bg-indigo-50/60" : "border-indigo-200 bg-white/60"
        )}
      >
        <div className="mx-auto size-12 rounded-2xl bg-copilot-gradient grid place-items-center text-white shadow-md">
          <Upload className="size-5" />
        </div>
        <div className="mt-3 text-[14px] font-semibold">Drag & drop files or click to browse</div>
        <div className="text-[11px] text-muted-foreground mt-1">PPT · Word · Excel · PDF · Images · Videos</div>
        <input
          ref={fileInputRef}
          type="file"
          multiple
          onChange={(e) => {
            // Read the FileList before resetting the input, then reset so picking the
            // same file twice in a row still fires a change event.
            const picked = Array.from(e.target.files ?? []);
            e.target.value = "";
            if (picked.length) handleFiles(picked);
          }}
          className="hidden"
        />
        <Button className="mt-4 rounded-xl bg-copilot-gradient text-white" onClick={() => fileInputRef.current?.click()}>
          <Upload className="size-4" />
          Choose files
        </Button>
      </div>

      {files.length > 0 && (
        <div className="mt-4 space-y-2">
          {files.map((f: PendingFile) => {
            const Icon = fileIconFor(f.file.name);
            return (
              <div key={f.id} className="rounded-xl bg-white border border-black/5 p-3 flex items-center gap-3">
                <div className="size-9 rounded-lg bg-gradient-to-br from-indigo-500/15 to-fuchsia-500/15 grid place-items-center text-indigo-600">
                  <Icon className="size-4" />
                </div>
                <div className="flex-1 min-w-0">
                  <div className="text-[13px] font-medium truncate">{f.file.name}</div>
                  <div className="text-[11px] text-muted-foreground">{humanSize(f.file.size)}</div>
                </div>
                <span className="text-[10px] font-medium px-2 py-0.5 rounded-full bg-muted text-muted-foreground">
                  Uploads on save
                </span>
                <button onClick={() => onRemoveFile(f.id)} className="size-8 rounded-lg hover:bg-black/5 grid place-items-center text-muted-foreground">
                  <Trash2 className="size-4" />
                </button>
              </div>
            );
          })}
        </div>
      )}

      {/* Links */}
      <div className="mt-6">
        <Label className="text-[12px] font-semibold">Add a link</Label>
        <div className="mt-1.5 grid sm:grid-cols-[140px_1fr_1fr_auto] gap-2">
          <select
            value={linkSource}
            onChange={(e) => setLinkSource(e.target.value)}
            className="h-10 rounded-xl border border-input bg-white px-3 text-[13px]"
          >
            {LINK_SOURCES.map((s) => (
              <option key={s}>{s}</option>
            ))}
          </select>
          <Input value={linkUrl} onChange={(e) => setLinkUrl(e.target.value)} placeholder="https://…" className="h-10 bg-white rounded-xl" />
          <Input value={linkLabel} onChange={(e) => setLinkLabel(e.target.value)} placeholder="Label (optional)" className="h-10 bg-white rounded-xl" />
          <Button onClick={onAddLink} className="h-10 rounded-xl bg-copilot-gradient text-white">
            <Plus className="size-4" />
            Add
          </Button>
        </div>

        {links.length > 0 && (
          <div className="mt-3 space-y-2">
            {links.map((l: any) => (
              <div key={l.id} className="rounded-xl bg-white border border-black/5 p-3 flex items-center gap-3">
                <div className="size-8 rounded-lg bg-gradient-to-br from-sky-500 to-cyan-500 grid place-items-center text-white">
                  <LinkIcon className="size-4" />
                </div>
                <div className="flex-1 min-w-0">
                  <div className="text-[13px] font-medium truncate">{l.label}</div>
                  <div className="text-[11px] text-muted-foreground truncate">{l.source} · {l.url}</div>
                </div>
                <button onClick={() => onRemoveLink(l.id)} className="size-8 rounded-lg hover:bg-black/5 grid place-items-center text-muted-foreground">
                  <Trash2 className="size-4" />
                </button>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

/* Step 6 */
function Step6(props: any) {
  const { contributors, filteredPeople, personQuery, setPersonQuery, onAdd, onRemove, onTogglePrimary, setContributors } = props;
  return (
    <div>
      <SectionTitle eyebrow="Step 6" title="Contributors" description="Give credit where it's due. You're automatically included." />

      <Label className="text-[12px] font-semibold">Add contributors</Label>
      <div className="relative mt-1.5">
        <Input
          value={personQuery}
          onChange={(e) => setPersonQuery(e.target.value)}
          placeholder="Search people by name or role…"
          className="h-10 bg-white rounded-xl"
        />
      </div>

      {personQuery && (
        <div className="mt-2 rounded-xl bg-white border border-black/5 shadow-sm overflow-hidden max-h-56 overflow-y-auto">
          {filteredPeople.length === 0 ? (
            <div className="p-3 text-[12px] text-muted-foreground">No matches</div>
          ) : (
            filteredPeople.map((p: any) => (
              <button
                key={p.id}
                onClick={() => onAdd(p.id)}
                className="w-full flex items-center gap-3 p-2.5 hover:bg-indigo-50/60 text-left"
              >
                <div className="size-8 rounded-full bg-gradient-to-br from-indigo-500 to-fuchsia-500 grid place-items-center text-[10px] font-semibold text-white">
                  {p.name.split(" ").map((n: string) => n[0]).join("").slice(0, 2)}
                </div>
                <div className="flex-1 min-w-0">
                  <div className="text-[13px] font-medium truncate">{p.name}</div>
                  <div className="text-[11px] text-muted-foreground truncate">{p.subtitle}</div>
                </div>
                <Plus className="size-4 text-muted-foreground" />
              </button>
            ))
          )}
        </div>
      )}

      <div className="mt-5 space-y-2">
        {contributors.map((c: any) => (
          <div key={c.id} className="rounded-xl bg-white border border-black/5 p-3 flex items-center gap-3">
            <div className="size-9 rounded-full bg-gradient-to-br from-indigo-500 to-fuchsia-500 grid place-items-center text-[11px] font-semibold text-white">
              {c.name.split(" ").map((n: string) => n[0]).join("").slice(0, 2)}
            </div>
            <div className="flex-1 min-w-0">
              <div className="text-[13px] font-semibold truncate">{c.name}</div>
              <div className="text-[11px] text-muted-foreground truncate">{c.role}</div>
            </div>
            <Input
              value={c.area}
              onChange={(e) =>
                setContributors(contributors.map((x: any) => (x.id === c.id ? { ...x, area: e.target.value } : x)))
              }
              placeholder="Responsibility area"
              className="h-9 w-40 bg-white rounded-lg text-[12px]"
            />
            <button
              onClick={() => onTogglePrimary(c.id)}
              className={cn(
                "h-9 px-3 rounded-lg text-[11px] font-semibold border transition",
                c.primary ? "bg-copilot-gradient text-white border-transparent" : "bg-white border-black/5 text-muted-foreground"
              )}
            >
              {c.primary ? "Primary" : "Supporting"}
            </button>
            <button
              onClick={() => onRemove(c.id)}
              disabled={c.area === "Submitter"}
              className="size-8 rounded-lg hover:bg-black/5 grid place-items-center text-muted-foreground disabled:opacity-30"
              aria-label="Remove"
            >
              <Trash2 className="size-4" />
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}

/* Step 7 */
function Step7({ visibility, toggle }: { visibility: string[]; toggle: (v: string) => void }) {
  return (
    <div>
      <SectionTitle eyebrow="Step 7" title="Visibility & AI Reuse" description="Where can this content be reused? These are metadata only — nothing publishes automatically." />
      <div className="grid sm:grid-cols-2 md:grid-cols-3 gap-2">
        {REUSE_TARGETS.map((r) => {
          const active = visibility.includes(r);
          return (
            <button
              key={r}
              onClick={() => toggle(r)}
              className={cn(
                "flex items-center gap-2 rounded-xl border p-3 text-left transition",
                active ? "border-transparent ring-2 ring-indigo-500 bg-white shadow-sm" : "border-black/5 bg-white hover:border-indigo-200"
              )}
            >
              <div className={cn("size-6 rounded-md grid place-items-center", active ? "bg-copilot-gradient text-white" : "bg-muted text-muted-foreground")}>
                <Check className="size-3.5" />
              </div>
              <span className="text-[13px] font-medium">{r}</span>
            </button>
          );
        })}
      </div>
      <div className="mt-4 rounded-xl bg-indigo-50/60 border border-indigo-100 p-3 text-[11px] text-indigo-900 leading-relaxed">
        Selections above signal which surfaces the AI assistant may draw from. Publishing to newsletters, Viva Engage, and QBRs still requires explicit human approval.
      </div>
    </div>
  );
}

/* Step 8 */
function Step8(props: any) {
  const { initiativeName, workstream, types, title, description, keyTakeaway, priority, tags, files, links, contributors, visibility, onJumpTo } = props;
  return (
    <div>
      <SectionTitle eyebrow="Step 8" title="Review & Submit" description="Everything looks good? Submit to make this part of the Initiative's organizational memory." />

      <div className="space-y-3">
        <ReviewCard title="Initiative" step={1} onEdit={onJumpTo}>
          <div className="text-[13px] font-semibold">{initiativeName ?? "—"}</div>
          <div className="text-[11px] text-muted-foreground">{workstream ?? ""}</div>
        </ReviewCard>
        <ReviewCard title="Contribution type" step={2} onEdit={onJumpTo}>
          <div className="flex flex-wrap gap-1">
            {types.length ? types.map((t: string) => (
              <span key={t} className="text-[11px] font-medium px-2 py-0.5 rounded-full bg-copilot-gradient text-white">{t}</span>
            )) : <span className="text-[12px] text-muted-foreground">Not selected</span>}
          </div>
        </ReviewCard>
        <ReviewCard title="Summary" step={3} onEdit={onJumpTo}>
          <div className="text-[13px] font-semibold">{title || "—"}</div>
          <div className="text-[12px] text-muted-foreground mt-1 line-clamp-3">{description}</div>
          {keyTakeaway && <div className="text-[12px] mt-2"><span className="font-semibold">Takeaway:</span> {keyTakeaway}</div>}
          <div className="mt-2 flex items-center gap-2 flex-wrap">
            <span className="text-[10px] px-2 py-0.5 rounded-full bg-amber-500/10 text-amber-700 font-semibold">{priority} priority</span>
            {tags.map((t: string) => (
              <span key={t} className="text-[10px] px-2 py-0.5 rounded-full bg-white border border-black/5">{t}</span>
            ))}
          </div>
        </ReviewCard>
        <ReviewCard title="Evidence" step={5} onEdit={onJumpTo}>
          <div className="text-[12px] text-muted-foreground">{files.length} file{files.length === 1 ? "" : "s"} · {links.length} link{links.length === 1 ? "" : "s"}</div>
        </ReviewCard>
        <ReviewCard title="Contributors" step={6} onEdit={onJumpTo}>
          <div className="flex flex-wrap gap-2">
            {contributors.map((c: any) => (
              <div key={c.id} className="inline-flex items-center gap-1.5 text-[11px] bg-white border border-black/5 rounded-full px-2 py-0.5">
                <span className="size-4 rounded-full bg-gradient-to-br from-indigo-500 to-fuchsia-500" />
                {c.name} {c.primary && <span className="text-[9px] text-indigo-600 font-bold">·PRIMARY</span>}
              </div>
            ))}
          </div>
        </ReviewCard>
        <ReviewCard title="Reuse" step={7} onEdit={onJumpTo}>
          {visibility.length ? (
            <div className="flex flex-wrap gap-1">
              {visibility.map((v: string) => (
                <span key={v} className="text-[11px] px-2 py-0.5 rounded-full bg-indigo-50 text-indigo-700 border border-indigo-100">{v}</span>
              ))}
            </div>
          ) : (
            <span className="text-[12px] text-muted-foreground">No reuse targets selected</span>
          )}
        </ReviewCard>
      </div>
    </div>
  );
}

function ReviewCard({ title, step, onEdit, children }: any) {
  return (
    <div className="rounded-xl bg-white border border-black/5 p-4">
      <div className="flex items-center justify-between mb-2">
        <div className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">{title}</div>
        <button onClick={() => onEdit(step)} className="text-[11px] text-primary font-medium hover:underline">Edit</button>
      </div>
      {children}
    </div>
  );
}

function SuccessView({ initiativeName, onClose }: { initiativeName: string; onClose: () => void }) {
  return (
    <div className="h-full flex flex-col items-center justify-center p-8 text-center relative overflow-hidden">
      <div className="absolute inset-0 bg-mesh opacity-40 pointer-events-none" />
      <div className="relative size-20 rounded-3xl bg-copilot-gradient grid place-items-center text-white shadow-xl">
        <CheckCircle2 className="size-10" />
      </div>
      <h3 className="relative mt-5 text-2xl font-semibold tracking-tight">Contribution submitted 🎉</h3>
      <p className="relative text-muted-foreground mt-2 max-w-md">
        Added to <span className="font-semibold text-foreground">{initiativeName}</span>. It's now searchable, available to Copilot, and ready for Executive Briefs & QBRs.
      </p>
      <Button onClick={onClose} className="relative mt-6 rounded-xl bg-copilot-gradient text-white">
        Done
      </Button>
    </div>
  );
}
