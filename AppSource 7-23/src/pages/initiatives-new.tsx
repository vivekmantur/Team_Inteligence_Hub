import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { PageHeader } from "@/components/system/PageHeader";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import {
  Rocket,
  X,
  Plus,
  Users,
  Sparkles,
  CheckCircle2,
  Building2,
  Target,
  CalendarDays,
  Tag as TagIcon,
  ShieldCheck,
  FileText,
  ArrowLeft,
  Save,
  GitBranch,
  type LucideIcon,
} from "lucide-react";
import {
  useCreateInitiative,
  useUpdateInitiative,
  useInitiativeQuery,
  statusToLabel,
  statusToWire,
  workformToLabel,
  workformToWire,
  focusAreaToLabel,
  focusAreaToWire,
  lifecycleStageToLabel,
  lifecycleStageToWire,
  healthToLabel,
  healthToWire,
  changeImpactToWire,
  enterpriseRoleToLabel,
  WORKFORMS,
  FOCUS_AREAS,
  LIFECYCLE_STAGES,
  HEALTHS,
  STATUSES,
  CHANGE_IMPACTS,
  ENTERPRISE_ROLES,
  type EnterpriseRoleWire,
  type Initiative as SavedInitiative,
} from "@/hooks/use-initiatives-api";
import { useBackendUser } from "@/hooks/use-backend-user";
import { useUsers } from "@/hooks/use-users";
import { UserPicker } from "@/components/system/UserPicker";
import {
  useInitiatives,
  type CreatedInitiative,
} from "@/components/initiative/InitiativeContext";
import { useAddContribution } from "@/components/contribution/ContributionContext";

const statuses = STATUSES.map(statusToLabel);
type StatusValue = string;

const priorities = ["High", "Medium", "Low"] as const;
type PriorityValue = (typeof priorities)[number];

const initiativeTypes = WORKFORMS.map(workformToLabel);
const businessAreas = FOCUS_AREAS.map(focusAreaToLabel);
const lifecycleStages = LIFECYCLE_STAGES.map(lifecycleStageToLabel);
const healths = HEALTHS.map(healthToLabel);
const changeImpacts = CHANGE_IMPACTS;

const suggestedTags = [
  "Copilot",
  "Role Hub",
  "Executive",
  "APAC",
  "EMEA",
  "Playbook",
  "Enablement",
  "AI",
];

type SectionKey = "details" | "ownership" | "lifecycle" | "timeline" | "outcomes";

// Kept in step with the constants on the Initiative entity.
const NAME_MIN = 3;
const NAME_MAX = 255;
const DESCRIPTION_MIN = 10;
const DESCRIPTION_MAX = 4000;
const LONG_TEXT_MAX = 4000;
const MAX_DURATION_YEARS = 20;
const EARLIEST_START_DATE = "2000-01-01";

const statusToneMap: Record<string, string> = {
  Active: "bg-emerald-500/10 text-emerald-700",
  "On Hold": "bg-amber-500/10 text-amber-700",
  Completed: "bg-indigo-500/10 text-indigo-700",
  Cancelled: "bg-rose-500/10 text-rose-700",
};

const healthToneMap: Record<string, string> = {
  "On Track": "bg-emerald-500/10 text-emerald-700",
  "Needs Attention": "bg-amber-500/10 text-amber-700",
  "At Risk": "bg-rose-500/10 text-rose-700",
};

export default function NewInitiativePage() {
  const navigate = useNavigate();
  const { id: routeId } = useParams<{ id: string }>();
  const isEditMode = routeId !== undefined;
  const editingId = isEditMode && /^\d+$/.test(routeId) ? Number(routeId) : null;

  const { addInitiative } = useInitiatives();
  const { openAddContribution } = useAddContribution();
  const createInitiative = useCreateInitiative();
  const updateInitiative = useUpdateInitiative(editingId);
  const {
    data: existingInitiative,
    isLoading: isLoadingExisting,
    error: loadExistingError,
  } = useInitiativeQuery(editingId);
  const { data: currentUser } = useBackendUser();
  const { data: users = [] } = useUsers();

  // Section 1: Initiative Details
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [businessArea, setBusinessArea] = useState<string>(businessAreas[0]);
  const [initiativeType, setInitiativeType] = useState<string>(initiativeTypes[0]);
  const [priority, setPriority] = useState<PriorityValue>("Medium");

  // Section 2: Ownership & Impacted Audience
  // Owner and sponsor are user ids, not names — the Owner column is a foreign key.
  const [ownerUserId, setOwnerUserId] = useState<number | null>(null);
  const [executiveSponsorUserId, setExecutiveSponsorUserId] = useState<number | null>(null);
  const [impactedRoles, setImpactedRoles] = useState<EnterpriseRoleWire[]>([]);
  const [changeImpact, setChangeImpact] = useState<(typeof changeImpacts)[number]>("Medium");

  // Section 3: Lifecycle, Health & Status
  const [lifecycleStage, setLifecycleStage] = useState<string>(lifecycleStages[0]);
  const [health, setHealth] = useState<string>(healths[0]);
  const [status, setStatus] = useState<StatusValue>(statuses[0]);

  // Section 4: Timeline
  const [startDate, setStartDate] = useState<string>("");
  const [targetEndDate, setTargetEndDate] = useState<string>("");

  // Section 5: Outcomes
  const [keyObjective, setKeyObjective] = useState("");
  const [expectedOutcome, setExpectedOutcome] = useState("");
  const [successMeasures, setSuccessMeasures] = useState("");
  const [tagInput, setTagInput] = useState("");
  const [tags, setTags] = useState<string[]>([]);

  // Post-create state
  const [created, setCreated] = useState<CreatedInitiative | null>(null);

  // Whoever is creating the Initiative owns it until they pick someone else — only
  // applies when actually creating; an edit's Owner comes from the fetched record.
  useEffect(() => {
    if (isEditMode) return;
    if (currentUser && ownerUserId === null) {
      setOwnerUserId(currentUser.id);
    }
  }, [currentUser, ownerUserId, isEditMode]);

  // Fills every field from the fetched Initiative exactly once, so a later refetch (or
  // this effect re-running) never clobbers an edit the user is mid-way through making.
  const [hasHydrated, setHasHydrated] = useState(false);
  useEffect(() => {
    if (!isEditMode || !existingInitiative || hasHydrated) return;

    setName(existingInitiative.name);
    setDescription(existingInitiative.description);
    setBusinessArea(focusAreaToLabel(existingInitiative.businessArea));
    setInitiativeType(workformToLabel(existingInitiative.initiativeType));
    setPriority(existingInitiative.priority as PriorityValue);
    setOwnerUserId(existingInitiative.ownerUserId);
    setExecutiveSponsorUserId(existingInitiative.executiveSponsorUserId ?? null);
    setImpactedRoles(existingInitiative.impactedRoles);
    setChangeImpact(existingInitiative.changeImpact);
    setLifecycleStage(lifecycleStageToLabel(existingInitiative.lifecycleStage));
    setHealth(healthToLabel(existingInitiative.health));
    setStatus(statusToLabel(existingInitiative.status));
    setStartDate(existingInitiative.startDate);
    setTargetEndDate(existingInitiative.targetEndDate);
    setKeyObjective(existingInitiative.keyObjective ?? "");
    setExpectedOutcome(existingInitiative.expectedOutcome ?? "");
    setSuccessMeasures(existingInitiative.successMeasures ?? "");
    setHasHydrated(true);
  }, [isEditMode, existingInitiative, hasHydrated]);

  const nameOf = (id: number | null) =>
    users.find((u) => u.id === id)?.displayName ?? "";
  const [showValidation, setShowValidation] = useState(false);

  // Mirrors the rules in CreateInitiativeRequestDto and InitiativeService, so the
  // form rejects what the API would reject rather than waiting for a round trip.
  const errors = useMemo(() => {
    const e: Partial<Record<string, string>> = {};

    const trimmedName = name.trim();
    if (!trimmedName) {
      e.name = "Initiative Name is required.";
    } else if (trimmedName.length < NAME_MIN) {
      e.name = `Initiative Name must be at least ${NAME_MIN} characters.`;
    } else if (trimmedName.length > NAME_MAX) {
      e.name = `Initiative Name cannot exceed ${NAME_MAX} characters.`;
    }

    const trimmedDescription = description.trim();
    if (!trimmedDescription) {
      e.description = "Description is required.";
    } else if (trimmedDescription.length < DESCRIPTION_MIN) {
      e.description = `Description must be at least ${DESCRIPTION_MIN} characters.`;
    } else if (trimmedDescription.length > DESCRIPTION_MAX) {
      e.description = `Description cannot exceed ${DESCRIPTION_MAX} characters.`;
    }

    if (ownerUserId === null) e.owner = "Initiative Owner is required.";

    if (!startDate) {
      e.startDate = "Start Date is required.";
    } else if (startDate < EARLIEST_START_DATE) {
      e.startDate = `Start Date cannot be before ${EARLIEST_START_DATE}.`;
    }

    if (!targetEndDate) {
      e.targetEndDate = "Target End Date is required.";
    } else if (startDate && targetEndDate < startDate) {
      e.targetEndDate = "Target End Date must be on or after Start Date.";
    } else if (startDate) {
      const limit = new Date(startDate);
      limit.setFullYear(limit.getFullYear() + MAX_DURATION_YEARS);
      if (new Date(targetEndDate) > limit) {
        e.targetEndDate = `An Initiative cannot span more than ${MAX_DURATION_YEARS} years.`;
      }
    }

    return e;
  }, [name, description, ownerUserId, startDate, targetEndDate]);

  const isValid = Object.keys(errors).length === 0;

  // Progress across the five sections (weighted equally)
  const sectionProgress: Record<SectionKey, number> = {
    details:
      (name.trim() ? 1 : 0) * 0.5 + (description.trim() ? 1 : 0) * 0.5,
    ownership: ownerUserId !== null ? 1 : 0,
    lifecycle: 1,
    timeline: (startDate ? 0.5 : 0) + (targetEndDate ? 0.5 : 0),
    outcomes:
      (keyObjective ? 0.34 : 0) +
      (expectedOutcome ? 0.33 : 0) +
      (successMeasures ? 0.33 : 0),
  };
  const totalProgress = Math.round(
    (Object.values(sectionProgress).reduce((a, b) => a + b, 0) / 5) * 100
  );

  const addTag = (t: string) => {
    const v = t.trim();
    if (!v || tags.includes(v)) return;
    setTags((prev) => [...prev, v]);
  };
  const removeTag = (t: string) => setTags((prev) => prev.filter((x) => x !== t));

  /** Shapes the saved record for the in-memory views that still read from context. */
  const toLocalRecord = (
    saved: SavedInitiative,
    isDraft: boolean
  ): CreatedInitiative => ({
    id: String(saved.id),
    name: saved.name,
    description: saved.description,
    workstream: focusAreaToLabel(saved.businessArea),
    status: statusToLabel(saved.status) as CreatedInitiative["status"],
    progress: 0,
    owner: saved.ownerDisplayName ?? (nameOf(saved.ownerUserId) || "Unassigned"),
    updated: "just now",
    tags,
    initiativeType: workformToLabel(saved.initiativeType),
    priority: saved.priority as CreatedInitiative["priority"],
    executiveSponsor: saved.executiveSponsorDisplayName ?? undefined,
    segment: saved.segment,
    impactedRoles: saved.impactedRoles.map(enterpriseRoleToLabel),
    changeImpact: saved.changeImpact,
    startDate: saved.startDate,
    targetEndDate: saved.targetEndDate,
    lifecycleStage: lifecycleStageToLabel(saved.lifecycleStage),
    health: healthToLabel(saved.health),
    currentPhase: lifecycleStageToLabel(saved.lifecycleStage),
    keyObjective: saved.keyObjective ?? undefined,
    expectedOutcome: saved.expectedOutcome ?? undefined,
    successMeasures: saved.successMeasures ?? undefined,
    isDraft,
    createdAt: saved.createdAt,
  });

  /**
   * Sends the form to the API and keeps the local view in step with what was stored.
   *
   * "Draft" is a local-only distinction now — InitiativeStatus no longer has a Draft
   * member (it moved out to LifecycleStage/Health), so both buttons send whatever
   * Status/Lifecycle/Health the user actually chose; only the success-screen wording
   * differs. Description and both dates are NOT NULL in the database either way, so a
   * draft cannot skip them.
   */
  const submit = async (isDraft: boolean) => {
    setShowValidation(true);

    if (!isValid) return;

    const payload = {
      name: name.trim(),
      description: description.trim(),
      businessArea: focusAreaToWire(businessArea),
      initiativeType: workformToWire(initiativeType),
      priority,
      ownerUserId,
      executiveSponsorUserId,
      impactedRoles,
      changeImpact: changeImpactToWire(changeImpact),
      startDate,
      targetEndDate,
      lifecycleStage: lifecycleStageToWire(lifecycleStage),
      health: healthToWire(health),
      status: statusToWire(status),
      keyObjective: keyObjective.trim() || undefined,
      expectedOutcome: expectedOutcome.trim() || undefined,
      successMeasures: successMeasures.trim() || undefined,
    };

    try {
      if (isEditMode && editingId !== null) {
        await updateInitiative.mutateAsync(payload);
        navigate(`/initiatives/${editingId}`);
        return;
      }

      const saved = await createInitiative.mutateAsync(payload);

      const record = toLocalRecord(saved, isDraft);
      addInitiative(record);
      setCreated(record);
    } catch {
      // Rendered from activeError below.
    }
  };

  const handleSaveDraft = () => void submit(true);
  const handleCreate = () => void submit(false);
  const handleSaveChanges = () => void submit(false);

  const handleCancel = () =>
    navigate(isEditMode && editingId !== null ? `/initiatives/${editingId}` : "/initiatives");

  const activeError = isEditMode ? updateInitiative.error : createInitiative.error;
  const isSubmitting = isEditMode ? updateInitiative.isPending : createInitiative.isPending;

  // === Edit mode: loading / not-found guards (error checked first, since it also
  //     leaves isLoading false and hasHydrated false — the same shape as "still
  //     loading" otherwise) ===
  if (isEditMode && (loadExistingError || (!isLoadingExisting && !existingInitiative))) {
    return (
      <div className="max-w-3xl mx-auto py-16 text-center">
        <p className="text-muted-foreground">
          {loadExistingError?.message ?? "This Initiative could not be found."}
        </p>
        <Button
          variant="outline"
          className="mt-4 rounded-xl"
          onClick={() => navigate("/initiatives")}
        >
          <ArrowLeft className="size-4" /> Back to Initiatives
        </Button>
      </div>
    );
  }

  if (isEditMode && (isLoadingExisting || !hasHydrated)) {
    return (
      <div className="max-w-3xl mx-auto py-16 text-center text-muted-foreground">
        Loading Initiative…
      </div>
    );
  }

  // === Success screen (create only — edit navigates straight to the Initiative) ===
  if (created) {
    return (
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="glass rounded-3xl p-8 md:p-10 relative overflow-hidden text-center">
          <div className="absolute inset-0 bg-mesh opacity-40 pointer-events-none" />
          <div className="absolute -top-20 -right-20 size-64 rounded-full bg-copilot-gradient opacity-25 blur-3xl" />
          <div className="relative">
            <div className="mx-auto size-16 rounded-2xl bg-copilot-gradient grid place-items-center shadow-xl">
              <CheckCircle2 className="size-8 text-white" />
            </div>
            <h2 className="mt-4 text-2xl md:text-3xl font-semibold tracking-tight">
              Initiative created successfully
            </h2>
            <p className="mt-2 text-muted-foreground">
              <span className="font-medium text-foreground">{created.name}</span>{" "}
              is now part of your organizational memory.
              {created.isDraft ? " Saved as a draft." : ""}
            </p>

            <div className="mt-6 grid sm:grid-cols-3 gap-3 max-w-2xl mx-auto text-left">
              <div className="rounded-xl bg-white/70 p-3 border border-white/60">
                <div className="text-[10px] uppercase tracking-wider text-muted-foreground">Owner</div>
                <div className="text-sm font-semibold mt-0.5">{created.owner}</div>
              </div>
              <div className="rounded-xl bg-white/70 p-3 border border-white/60">
                <div className="text-[10px] uppercase tracking-wider text-muted-foreground">Business Area</div>
                <div className="text-sm font-semibold mt-0.5">{created.workstream}</div>
              </div>
              <div className="rounded-xl bg-white/70 p-3 border border-white/60">
                <div className="text-[10px] uppercase tracking-wider text-muted-foreground">Type</div>
                <div className="text-sm font-semibold mt-0.5">{created.initiativeType}</div>
              </div>
            </div>

            <div className="mt-8 flex flex-wrap gap-2 justify-center">
              <Button
                variant="outline"
                className="rounded-xl bg-white/80"
                onClick={() => navigate(`/initiatives/${created.id}`)}
              >
                <Users className="size-4" /> Manage Team
              </Button>
              <Button
                variant="outline"
                className="rounded-xl bg-white/80"
                onClick={() => openAddContribution({ initiativeId: created.id })}
              >
                <Sparkles className="size-4" /> Add Contribution
              </Button>
              <Button
                className="rounded-xl bg-copilot-gradient text-white shadow-lg"
                onClick={() => navigate(`/initiatives/${created.id}`)}
              >
                <Rocket className="size-4" /> View Initiative
              </Button>
            </div>

            <div className="mt-6 text-[12px] text-muted-foreground">
              <span className="font-medium">Next up:</span> team assignment, contributions, assets, and metrics
              are added from the Initiative Detail page and Add Contribution flow.
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="pb-28">
      <PageHeader
        eyebrow={isEditMode ? "Edit" : "Create"}
        title={isEditMode ? "Edit Initiative" : "New Initiative"}
        description={
          isEditMode
            ? "Update the Initiative's details. Team, contributions, assets, and metrics are managed from the Initiative Detail page."
            : "Capture the essentials to establish the Initiative as a primary record. Team, contributions, assets, and metrics are added after creation."
        }
        actions={
          <Button variant="ghost" className="rounded-xl" onClick={handleCancel}>
            <ArrowLeft className="size-4" /> {isEditMode ? "Back to Initiative" : "Back to Initiatives"}
          </Button>
        }
      />

      <div className="grid lg:grid-cols-[1fr_320px] gap-6">
        {/* Main form */}
        <div className="space-y-5">
          {/* Section 1: Initiative Details */}
          <FormSection
            icon={FileText}
            step={1}
            title="Initiative Details"
            subtitle="The basics that identify this Initiative."
            progress={sectionProgress.details}
          >
            <div className="grid gap-4">
              <Field
                label="Initiative Name"
                required
                error={showValidation ? errors.name : undefined}
              >
                <Input
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="e.g. Role Hub Global Rollout"
                  className="h-11 rounded-xl bg-white/80 border-white/70"
                />
              </Field>

              <Field
                label="Description"
                required
                error={showValidation ? errors.description : undefined}
                hint="A short summary of what this Initiative is about."
              >
                <Textarea
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="What is this Initiative delivering? Who benefits?…"
                  rows={3}
                  className="rounded-xl bg-white/80 border-white/70 resize-none"
                />
              </Field>

              <div className="grid md:grid-cols-2 gap-4">
                <Field label="What area of work does the initiative primarily support?" required>
                  <select
                    value={businessArea}
                    onChange={(e) => setBusinessArea(e.target.value)}
                    className="h-11 rounded-xl border border-white/70 bg-white/80 px-3 text-sm w-full"
                  >
                    {businessAreas.map((a) => (
                      <option key={a} value={a}>
                        {a}
                      </option>
                    ))}
                  </select>
                </Field>
                <Field label="What form does the work take?" required>
                  <select
                    value={initiativeType}
                    onChange={(e) => setInitiativeType(e.target.value)}
                    className="h-11 rounded-xl border border-white/70 bg-white/80 px-3 text-sm w-full"
                  >
                    {initiativeTypes.map((t) => (
                      <option key={t} value={t}>
                        {t}
                      </option>
                    ))}
                  </select>
                </Field>
              </div>

              <Field label="Priority">
                <div className="flex gap-2">
                  {priorities.map((p) => (
                    <button
                      key={p}
                      type="button"
                      onClick={() => setPriority(p)}
                      className={cn(
                        "flex-1 rounded-xl border h-11 text-sm font-medium transition inline-flex items-center justify-center gap-2",
                        priority === p
                          ? "bg-copilot-gradient text-white border-transparent shadow-sm"
                          : "bg-white/70 border-white/70 hover:bg-white"
                      )}
                    >
                      <span
                        className={cn(
                          "size-2 rounded-full",
                          p === "High" && "bg-rose-500",
                          p === "Medium" && "bg-amber-500",
                          p === "Low" && "bg-emerald-500"
                        )}
                      />
                      {p}
                    </button>
                  ))}
                </div>
              </Field>
            </div>
          </FormSection>

          {/* Section 2: Ownership & Impacted Audience */}
          <FormSection
            icon={ShieldCheck}
            step={2}
            title="Ownership & Impacted Audience"
            subtitle="Keep initiative-team ownership separate from the Enterprise roles impacted by the change."
            progress={sectionProgress.ownership}
          >
            <div className="grid gap-4">
              <div className="grid md:grid-cols-2 gap-4">
                <Field
                  label="Initiative Owner"
                  required
                  error={showValidation ? errors.owner : undefined}
                >
                  <UserPicker
                    value={ownerUserId}
                    onChange={setOwnerUserId}
                    placeholder="Search people who have signed in…"
                  />
                </Field>
                <Field label="Executive Sponsor" hint="Optional — leadership backer.">
                  <UserPicker
                    value={executiveSponsorUserId}
                    onChange={setExecutiveSponsorUserId}
                    placeholder="Search people who have signed in…"
                    excludeUserId={ownerUserId}
                  />
                </Field>
              </div>

              <Field label="Segment">
                <div className="inline-flex items-center rounded-xl border border-indigo-200 bg-indigo-500/10 px-3 h-10 text-[13px] font-semibold text-indigo-700">
                  Enterprise
                </div>
              </Field>

              <Field
                label="Impacted Roles"
                hint="Select one, multiple, or all Enterprise roles."
              >
                <div className="grid gap-2">
                  <label className="flex items-center gap-2.5 rounded-xl border border-white/70 bg-white/70 hover:bg-white transition p-3 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={impactedRoles.length === ENTERPRISE_ROLES.length}
                      onChange={(e) =>
                        setImpactedRoles(e.target.checked ? [...ENTERPRISE_ROLES] : [])
                      }
                      className="size-4 rounded accent-primary"
                    />
                    <span className="text-[13px] font-semibold">
                      Select all Enterprise roles
                    </span>
                  </label>
                  <div className="grid sm:grid-cols-2 gap-2">
                    {ENTERPRISE_ROLES.map((role) => {
                      const checked = impactedRoles.includes(role);
                      return (
                        <label
                          key={role}
                          className="flex items-center gap-2.5 rounded-xl border border-white/70 bg-white/70 hover:bg-white transition p-3 cursor-pointer"
                        >
                          <input
                            type="checkbox"
                            checked={checked}
                            onChange={(e) =>
                              setImpactedRoles((prev) =>
                                e.target.checked
                                  ? [...prev, role]
                                  : prev.filter((r) => r !== role)
                              )
                            }
                            className="size-4 rounded accent-primary"
                          />
                          <div className="min-w-0">
                            <div className="text-[13px] font-semibold">{role}</div>
                            <div className="text-[11px] text-muted-foreground truncate">
                              {enterpriseRoleToLabel(role)}
                            </div>
                          </div>
                        </label>
                      );
                    })}
                  </div>
                </div>
              </Field>

              <Field label="Change Impact" hint="Optional initial indication of the level of impact.">
                <div className="flex gap-2">
                  {changeImpacts.map((c) => (
                    <button
                      key={c}
                      type="button"
                      onClick={() => setChangeImpact(c)}
                      className={cn(
                        "flex-1 rounded-xl border h-11 text-sm font-medium transition",
                        changeImpact === c
                          ? "bg-copilot-gradient text-white border-transparent shadow-sm"
                          : "bg-white/70 border-white/70 hover:bg-white"
                      )}
                    >
                      {c}
                    </button>
                  ))}
                </div>
              </Field>
            </div>
          </FormSection>

          {/* Section 3: Lifecycle, Health & Status */}
          <FormSection
            icon={GitBranch}
            step={3}
            title="Lifecycle, Health & Status"
            subtitle="Track progression, performance and operational state independently."
            progress={sectionProgress.lifecycle}
          >
            <div className="grid gap-4">
              <Field label="Lifecycle Stage">
                <div className="flex flex-wrap gap-2">
                  {lifecycleStages.map((s) => (
                    <button
                      key={s}
                      type="button"
                      onClick={() => setLifecycleStage(s)}
                      className={cn(
                        "px-4 h-10 rounded-xl text-sm font-medium transition",
                        lifecycleStage === s
                          ? "bg-copilot-gradient text-white shadow-sm"
                          : "bg-white/70 border border-white/70 hover:bg-white"
                      )}
                    >
                      {s}
                    </button>
                  ))}
                </div>
              </Field>

              <Field label="Health">
                <div className="flex flex-wrap gap-2">
                  {healths.map((h) => (
                    <button
                      key={h}
                      type="button"
                      onClick={() => setHealth(h)}
                      className={cn(
                        "px-4 h-10 rounded-xl text-sm font-medium transition",
                        health === h
                          ? "bg-copilot-gradient text-white shadow-sm"
                          : "bg-white/70 border border-white/70 hover:bg-white"
                      )}
                    >
                      {h}
                    </button>
                  ))}
                </div>
                <span
                  className={cn(
                    "mt-2 inline-block px-2 py-0.5 rounded-full text-[11px] font-semibold",
                    healthToneMap[health]
                  )}
                >
                  {health}
                </span>
              </Field>

              <Field label="Status">
                <div className="flex flex-wrap gap-2">
                  {statuses.map((s) => (
                    <button
                      key={s}
                      type="button"
                      onClick={() => setStatus(s)}
                      className={cn(
                        "px-4 h-10 rounded-xl text-sm font-medium transition",
                        status === s
                          ? "bg-copilot-gradient text-white shadow-sm"
                          : "bg-white/70 border border-white/70 hover:bg-white"
                      )}
                    >
                      {s}
                    </button>
                  ))}
                </div>
                <span
                  className={cn(
                    "mt-2 inline-block px-2 py-0.5 rounded-full text-[11px] font-semibold",
                    statusToneMap[status]
                  )}
                >
                  {status}
                </span>
              </Field>
            </div>
          </FormSection>

          {/* Section 4: Timeline */}
          <FormSection
            icon={CalendarDays}
            step={4}
            title="Timeline"
            subtitle="When does this Initiative run? Progress starts at 0% and updates automatically as activity is captured."
            progress={sectionProgress.timeline}
          >
            <div className="grid md:grid-cols-2 gap-4">
              <Field
                label="Start Date"
                required
                error={showValidation ? errors.startDate : undefined}
              >
                <Input
                  type="date"
                  value={startDate}
                  onChange={(e) => setStartDate(e.target.value)}
                  className="h-11 rounded-xl bg-white/80 border-white/70"
                />
              </Field>
              <Field
                label="Target End Date"
                required
                error={showValidation ? errors.targetEndDate : undefined}
              >
                <Input
                  type="date"
                  value={targetEndDate}
                  onChange={(e) => setTargetEndDate(e.target.value)}
                  className="h-11 rounded-xl bg-white/80 border-white/70"
                />
              </Field>
            </div>
          </FormSection>

          {/* Section 5: Outcomes */}
          <FormSection
            icon={Target}
            step={5}
            title="Outcomes"
            subtitle="Help leadership and Copilot understand why this Initiative matters."
            progress={sectionProgress.outcomes}
          >
            <div className="grid gap-4">
              <Field label="Key Objective" hint="The single most important goal of this Initiative.">
                <Input
                  value={keyObjective}
                  onChange={(e) => setKeyObjective(e.target.value)}
                  placeholder="e.g. Reach 150K Role Hub MAU by end of FY26"
                  className="h-11 rounded-xl bg-white/80 border-white/70"
                />
              </Field>
              <Field label="Expected Outcome">
                <Textarea
                  value={expectedOutcome}
                  onChange={(e) => setExpectedOutcome(e.target.value)}
                  placeholder="What does success look like? Who benefits?…"
                  rows={3}
                  className="rounded-xl bg-white/80 border-white/70 resize-none"
                />
              </Field>
              <Field label="Success Measures / KPIs" hint="Comma-separate the metrics you’ll track.">
                <Textarea
                  value={successMeasures}
                  onChange={(e) => setSuccessMeasures(e.target.value)}
                  placeholder="e.g. MAU, adoption %, NPS, hours saved"
                  rows={2}
                  className="rounded-xl bg-white/80 border-white/70 resize-none"
                />
              </Field>

              <Field label="Tags" hint="Add multiple tags for classification and search.">
                <div className="rounded-xl bg-white/80 border border-white/70 p-2 flex flex-wrap gap-1.5">
                  {tags.map((t) => (
                    <span
                      key={t}
                      className="inline-flex items-center gap-1 text-[12px] font-medium bg-copilot-gradient text-white rounded-full pl-2.5 pr-1 py-0.5"
                    >
                      {t}
                      <button
                        type="button"
                        onClick={() => removeTag(t)}
                        className="size-4 rounded-full grid place-items-center hover:bg-white/20"
                        aria-label={`Remove ${t}`}
                      >
                        <X className="size-3" />
                      </button>
                    </span>
                  ))}
                  <input
                    value={tagInput}
                    onChange={(e) => setTagInput(e.target.value)}
                    onKeyDown={(e) => {
                      if (e.key === "Enter" || e.key === ",") {
                        e.preventDefault();
                        addTag(tagInput);
                        setTagInput("");
                      }
                    }}
                    placeholder={tags.length ? "" : "Type a tag and press Enter…"}
                    className="flex-1 min-w-[140px] outline-none bg-transparent text-sm px-1 py-1"
                  />
                </div>
                <div className="mt-2 flex flex-wrap gap-1.5">
                  {suggestedTags
                    .filter((s) => !tags.includes(s))
                    .map((s) => (
                      <button
                        key={s}
                        type="button"
                        onClick={() => addTag(s)}
                        className="text-[11px] px-2 py-0.5 rounded-full bg-white/70 border border-white/60 hover:bg-white inline-flex items-center gap-1"
                      >
                        <Plus className="size-3" /> {s}
                      </button>
                    ))}
                </div>
              </Field>
            </div>
          </FormSection>

          {/* Post-creation info card */}
          <div className="rounded-2xl bg-gradient-to-r from-indigo-50 via-white to-fuchsia-50 border border-indigo-100 p-4 flex items-center gap-3">
            <div className="size-9 rounded-xl bg-copilot-gradient grid place-items-center text-white shadow-sm shrink-0">
              <Sparkles className="size-4" />
            </div>
            <div className="flex-1 min-w-0">
              <div className="text-[13px] font-semibold">Team, contributions, assets, and metrics come next</div>
              <div className="text-[11px] text-muted-foreground">
                You’ll add these from the Initiative Detail page and Add Contribution flow after this is created.
              </div>
            </div>
          </div>
        </div>

        {/* Right-side summary panel */}
        <aside className="hidden lg:block space-y-4">
          <div className="glass rounded-2xl p-5">
            <div className="flex items-center gap-2">
              <div className="size-9 rounded-xl bg-copilot-gradient grid place-items-center text-white shadow-sm">
                <Rocket className="size-4" />
              </div>
              <div className="text-sm font-semibold">Initiative model</div>
            </div>
            <ul className="mt-3 space-y-2 text-[11px] text-muted-foreground leading-relaxed">
              <li><span className="font-semibold text-foreground">Lifecycle</span> shows where the initiative is in the change journey.</li>
              <li><span className="font-semibold text-foreground">Health</span> shows whether execution needs attention.</li>
              <li><span className="font-semibold text-foreground">Status</span> shows whether work is active, paused, cancelled, or complete.</li>
              <li><span className="font-semibold text-foreground">Impacted Roles</span> describe the Enterprise audiences affected, not the project team.</li>
            </ul>
          </div>

          <div className="glass rounded-2xl p-5 sticky top-24">
            <div className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider">
              Preview
            </div>
            <div className="text-sm font-semibold mt-0.5">{name || "Untitled Initiative"}</div>

            <div className="mt-4">
              <div className="flex items-center justify-between text-[11px] text-muted-foreground mb-1">
                <span>Completion</span>
                <span>{totalProgress}%</span>
              </div>
              <div className="h-1.5 rounded-full bg-muted overflow-hidden">
                <div
                  className="h-full bg-copilot-gradient transition-all"
                  style={{ width: `${totalProgress}%` }}
                />
              </div>
            </div>

            <div className="mt-4 grid grid-cols-2 gap-x-3 gap-y-2">
              <SummaryStat label="Lifecycle" value={lifecycleStage} />
              <SummaryStat label="Health" value={health} />
              <SummaryStat label="Status" value={status} />
              <SummaryStat label="Impact" value={changeImpact} />
            </div>

            <ul className="mt-4 space-y-2 text-[12px]">
              <SummaryRow label="Name" value={name || "—"} icon={FileText} />
              <SummaryRow label="Owner" value={nameOf(ownerUserId) || "—"} icon={Users} />
              <SummaryRow label="Business Area" value={businessArea} icon={Building2} />
              <SummaryRow label="Type" value={initiativeType} icon={TagIcon} />
              <SummaryRow
                label="Timeline"
                value={
                  startDate && targetEndDate
                    ? `${startDate} → ${targetEndDate}`
                    : "Not set"
                }
                icon={CalendarDays}
              />
            </ul>

            <div className="mt-3 text-[11px] text-muted-foreground">
              {impactedRoles.length === 0
                ? "No impacted roles selected yet."
                : `${impactedRoles.length} impacted role${impactedRoles.length === 1 ? "" : "s"} selected.`}
            </div>

            {tags.length > 0 && (
              <div className="mt-4">
                <div className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider mb-1.5">
                  Tags
                </div>
                <div className="flex flex-wrap gap-1.5">
                  {tags.map((t) => (
                    <Badge
                      key={t}
                      variant="secondary"
                      className="rounded-full text-[10px] font-medium"
                    >
                      {t}
                    </Badge>
                  ))}
                </div>
              </div>
            )}

            <div className="mt-5 text-[11px] text-muted-foreground leading-relaxed">
              After creation you’ll be able to manage the team, add contributions, upload assets, and track metrics.
            </div>
          </div>
        </aside>
      </div>

      {/* Sticky footer */}
      <div className="fixed bottom-0 left-0 right-0 lg:left-64 z-30 px-4 lg:px-6 pb-4">
        <div className="glass rounded-2xl px-4 py-3 flex flex-wrap items-center gap-2">
          <div className="flex items-center gap-3 min-w-0 flex-1">
            <div className="hidden md:block relative w-40">
              <div className="h-1.5 rounded-full bg-muted overflow-hidden">
                <div
                  className="h-full bg-copilot-gradient transition-all"
                  style={{ width: `${totalProgress}%` }}
                />
              </div>
            </div>
            <div className="text-[12px] text-muted-foreground truncate">
              {isValid
                ? "All required fields are complete."
                : `${Object.keys(errors).length} required field${Object.keys(errors).length === 1 ? "" : "s"} to complete.`}
            </div>
            {activeError && (
              <div className="mt-1 text-[12px] text-rose-700">
                {activeError.message}
              </div>
            )}
          </div>
          <div className="flex gap-2 flex-wrap">
            <Button variant="ghost" className="rounded-xl" onClick={handleCancel}>
              Cancel
            </Button>
            {!isEditMode && (
              <Button
                variant="outline"
                className="rounded-xl bg-white/80"
                onClick={handleSaveDraft}
                disabled={isSubmitting}
              >
                <Save className="size-4" /> Save as Draft
              </Button>
            )}
            <Button
              onClick={isEditMode ? handleSaveChanges : handleCreate}
              disabled={(!isValid && showValidation) || isSubmitting}
              className="rounded-xl bg-copilot-gradient text-white shadow-lg"
            >
              {isSubmitting ? (
                <>
                  <span className="size-4 rounded-full border-2 border-current border-t-transparent animate-spin" />
                  Saving…
                </>
              ) : isEditMode ? (
                <>
                  <Save className="size-4" /> Save Changes
                </>
              ) : (
                <>
                  <Rocket className="size-4" /> Create Initiative
                </>
              )}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}

/* ---------- Sub-components ---------- */

function FormSection({
  icon: Icon,
  step,
  title,
  subtitle,
  progress,
  children,
}: {
  icon: LucideIcon;
  step: number;
  title: string;
  subtitle?: string;
  progress: number;
  children: React.ReactNode;
}) {
  const complete = progress >= 0.999;
  return (
    <section className="glass rounded-2xl p-5">
      <div className="flex items-start gap-3">
        <div
          className={cn(
            "size-10 rounded-xl grid place-items-center text-white shadow-md shrink-0",
            complete
              ? "bg-gradient-to-br from-emerald-500 to-teal-500"
              : "bg-copilot-gradient"
          )}
        >
          {complete ? <CheckCircle2 className="size-5" /> : <Icon className="size-5" />}
        </div>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <span className="text-[10px] font-semibold text-muted-foreground uppercase tracking-wider">
              Step {step}
            </span>
            {complete && (
              <span className="text-[10px] font-semibold text-emerald-700 bg-emerald-500/10 rounded-full px-2 py-0.5">
                Complete
              </span>
            )}
          </div>
          <h3 className="font-semibold leading-tight">{title}</h3>
          {subtitle && (
            <p className="text-[12px] text-muted-foreground mt-0.5">{subtitle}</p>
          )}
        </div>
      </div>
      <div className="mt-4">{children}</div>
    </section>
  );
}

function Field({
  label,
  required,
  hint,
  error,
  children,
}: {
  label: string;
  required?: boolean;
  hint?: string;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <div className="flex items-center justify-between">
        <Label className="text-[12px] font-semibold">
          {label}
          {required && <span className="text-rose-500 ml-0.5">*</span>}
        </Label>
      </div>
      <div className="mt-1.5">{children}</div>
      {error ? (
        <div className="mt-1 text-[11px] text-rose-600">{error}</div>
      ) : hint ? (
        <div className="mt-1 text-[11px] text-muted-foreground">{hint}</div>
      ) : null}
    </div>
  );
}


function SummaryStat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg bg-white/60 px-2.5 py-2">
      <div className="text-[10px] uppercase tracking-wider text-muted-foreground">{label}</div>
      <div className="text-[12px] font-semibold truncate">{value}</div>
    </div>
  );
}

function SummaryRow({
  label,
  value,
  icon: Icon,
}: {
  label: string;
  value: string;
  icon: LucideIcon;
}) {
  return (
    <li className="flex items-start gap-2 rounded-lg bg-white/60 px-2.5 py-2">
      <div className="size-6 rounded-md bg-white grid place-items-center text-muted-foreground border border-white/70 shrink-0">
        <Icon className="size-3.5" />
      </div>
      <div className="min-w-0 flex-1">
        <div className="text-[10px] uppercase tracking-wider text-muted-foreground">{label}</div>
        <div className="text-[12px] font-medium truncate">{value}</div>
      </div>
    </li>
  );
}
