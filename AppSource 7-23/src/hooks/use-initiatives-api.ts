import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch, isApiConfigured } from "@/lib/api-client";
import { useAuth } from "./use-auth";

/**
 * The API speaks enum names; the UI shows display labels. These two vocabularies differ
 * only where a label contains a space, and translating between them is this module's job
 * so no component has to know the wire format.
 */
export type PriorityWire = "High" | "Medium" | "Low";

/** Whether work is active, paused, cancelled, or complete. */
export type StatusWire = "Active" | "OnHold" | "Cancelled" | "Completed";

/** What form the Initiative's work takes. */
export type WorkformWire =
  | "Motion"
  | "Rollout"
  | "Pilot"
  | "Campaign"
  | "ResearchOrAssessment"
  | "ContentDevelopment"
  | "OperationalImprovement"
  | "ReportingOrAnalytics"
  | "CommunityOrEngagementMotion";

/** The area of work an Initiative primarily supports. */
export type FocusAreaWire =
  | "AiTransformation"
  | "ChangeManagementAndAdoption"
  | "Enablement"
  | "InsightsAndMeasurement"
  | "StorytellingAndEvidence"
  | "StrategicPrograms"
  | "Other";

/** Where in the change journey an Initiative sits. */
export type LifecycleStageWire = "Assess" | "Plan" | "Activate" | "Measure" | "Sustain";

/** Whether execution needs attention, independent of lifecycle stage. */
export type HealthWire = "OnTrack" | "NeedsAttention" | "AtRisk";

/** Which audience segment an Initiative targets. */
export type SegmentWire = "Enterprise";

/** Initial indication of how disruptive an Initiative's change is expected to be. */
export type ChangeImpactWire = "Low" | "Medium" | "High";

/** An enterprise role an Initiative's change can land on. */
export type EnterpriseRoleWire = "AE" | "ATS" | "SSP" | "SE" | "CE" | "CSA" | "CSAM";

const STATUS_LABELS: Record<StatusWire, string> = {
  Active: "Active",
  OnHold: "On Hold",
  Cancelled: "Cancelled",
  Completed: "Completed",
};

const WORKFORM_LABELS: Record<WorkformWire, string> = {
  Motion: "Motion",
  Rollout: "Rollout",
  Pilot: "Pilot",
  Campaign: "Campaign",
  ResearchOrAssessment: "Research or Assessment",
  ContentDevelopment: "Content Development",
  OperationalImprovement: "Operational Improvement",
  ReportingOrAnalytics: "Reporting or Analytics",
  CommunityOrEngagementMotion: "Community or Engagement Motion",
};

const FOCUS_AREA_LABELS: Record<FocusAreaWire, string> = {
  AiTransformation: "AI Transformation",
  ChangeManagementAndAdoption: "Change Management & Adoption",
  Enablement: "Enablement",
  InsightsAndMeasurement: "Insights & Measurement",
  StorytellingAndEvidence: "Storytelling & Evidence",
  StrategicPrograms: "Strategic Programs",
  Other: "Other",
};

const LIFECYCLE_STAGE_LABELS: Record<LifecycleStageWire, string> = {
  Assess: "Assess",
  Plan: "Plan",
  Activate: "Activate",
  Measure: "Measure",
  Sustain: "Sustain",
};

const HEALTH_LABELS: Record<HealthWire, string> = {
  OnTrack: "On Track",
  NeedsAttention: "Needs Attention",
  AtRisk: "At Risk",
};

const SEGMENT_LABELS: Record<SegmentWire, string> = {
  Enterprise: "Enterprise",
};

const CHANGE_IMPACT_LABELS: Record<ChangeImpactWire, string> = {
  Low: "Low",
  Medium: "Medium",
  High: "High",
};

const ENTERPRISE_ROLE_LABELS: Record<EnterpriseRoleWire, string> = {
  AE: "Account Executive",
  ATS: "Account Technology Strategist",
  SSP: "Solution Sales Professional",
  SE: "Solution Engineer",
  CE: "Commercial Executive",
  CSA: "Cloud Solution Architect",
  CSAM: "Customer Success Account Manager",
};

function toWire<T extends string>(
  labels: Record<T, string>,
  label: string,
  fallback: T,
): T {
  const match = (Object.keys(labels) as T[]).find((key) => labels[key] === label);
  return match ?? fallback;
}

export const statusToWire = (label: string): StatusWire =>
  toWire(STATUS_LABELS, label, "Active");

export const statusToLabel = (wire: StatusWire): string =>
  STATUS_LABELS[wire] ?? wire;

export const workformToWire = (label: string): WorkformWire =>
  toWire(WORKFORM_LABELS, label, "Motion");

export const workformToLabel = (wire: WorkformWire): string =>
  WORKFORM_LABELS[wire] ?? wire;

export const focusAreaToWire = (label: string): FocusAreaWire =>
  toWire(FOCUS_AREA_LABELS, label, "Other");

export const focusAreaToLabel = (wire: FocusAreaWire): string =>
  FOCUS_AREA_LABELS[wire] ?? wire;

export const lifecycleStageToWire = (label: string): LifecycleStageWire =>
  toWire(LIFECYCLE_STAGE_LABELS, label, "Assess");

export const lifecycleStageToLabel = (wire: LifecycleStageWire): string =>
  LIFECYCLE_STAGE_LABELS[wire] ?? wire;

export const healthToWire = (label: string): HealthWire =>
  toWire(HEALTH_LABELS, label, "OnTrack");

export const healthToLabel = (wire: HealthWire): string =>
  HEALTH_LABELS[wire] ?? wire;

export const segmentToWire = (label: string): SegmentWire =>
  toWire(SEGMENT_LABELS, label, "Enterprise");

export const segmentToLabel = (wire: SegmentWire): string =>
  SEGMENT_LABELS[wire] ?? wire;

export const changeImpactToWire = (label: string): ChangeImpactWire =>
  toWire(CHANGE_IMPACT_LABELS, label, "Medium");

export const changeImpactToLabel = (wire: ChangeImpactWire): string =>
  CHANGE_IMPACT_LABELS[wire] ?? wire;

export const enterpriseRoleToLabel = (wire: EnterpriseRoleWire): string =>
  ENTERPRISE_ROLE_LABELS[wire] ?? wire;

export const ENTERPRISE_ROLES = Object.keys(ENTERPRISE_ROLE_LABELS) as EnterpriseRoleWire[];
export const WORKFORMS = Object.keys(WORKFORM_LABELS) as WorkformWire[];
export const FOCUS_AREAS = Object.keys(FOCUS_AREA_LABELS) as FocusAreaWire[];
export const LIFECYCLE_STAGES = Object.keys(LIFECYCLE_STAGE_LABELS) as LifecycleStageWire[];
export const HEALTHS = Object.keys(HEALTH_LABELS) as HealthWire[];
export const STATUSES = Object.keys(STATUS_LABELS) as StatusWire[];
export const CHANGE_IMPACTS = Object.keys(CHANGE_IMPACT_LABELS) as ChangeImpactWire[];

/** Mirrors InitiativeResponseDto from TeamIntelligenceHub.Application. */
export type Initiative = {
  id: number;
  name: string;
  description: string;
  businessArea: FocusAreaWire;
  initiativeType: WorkformWire;
  priority: PriorityWire;
  ownerUserId: number;
  ownerDisplayName: string | null;
  executiveSponsorUserId: number | null;
  executiveSponsorDisplayName: string | null;
  segment: SegmentWire;
  impactedRoles: EnterpriseRoleWire[];
  changeImpact: ChangeImpactWire;
  startDate: string;
  targetEndDate: string;
  lifecycleStage: LifecycleStageWire;
  health: HealthWire;
  status: StatusWire;
  keyObjective: string | null;
  expectedOutcome: string | null;
  successMeasures: string | null;
  createdAt: string;
  updatedAt: string;
};

/** Mirrors CreateInitiativeRequestDto. Dates are `yyyy-MM-dd`, which binds to DateOnly. */
export type CreateInitiativeRequest = {
  name: string;
  description: string;
  businessArea: FocusAreaWire;
  initiativeType: WorkformWire;
  priority?: PriorityWire;
  ownerUserId?: number | null;
  executiveSponsorUserId?: number | null;
  segment?: SegmentWire;
  impactedRoles?: EnterpriseRoleWire[];
  changeImpact?: ChangeImpactWire;
  startDate: string;
  targetEndDate: string;
  lifecycleStage?: LifecycleStageWire;
  health?: HealthWire;
  status?: StatusWire;
  keyObjective?: string;
  expectedOutcome?: string;
  successMeasures?: string;
};

export const initiativesQueryKey = ["initiatives"] as const;

export function useInitiativesQuery() {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: initiativesQueryKey,
    queryFn: () => apiFetch<Initiative[]>("/api/initiatives"),
    enabled: isAuthenticated && isApiConfigured,
  });
}

/**
 * Loads one Initiative by its numeric id.
 *
 * A non-numeric id (a legacy mock key such as "ini_9f2c") is treated as absent rather
 * than sent to the API, which would only answer 400.
 */
export function useInitiativeQuery(id: number | null) {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: [...initiativesQueryKey, id] as const,
    queryFn: () => apiFetch<Initiative>(`/api/initiatives/${id}`),
    enabled: isAuthenticated && isApiConfigured && id !== null,
  });
}

/** One role's coverage row, mirroring EnterpriseRoleCoverageDto. */
export type EnterpriseRoleCoverageRecord = {
  code: EnterpriseRoleWire;
  initiativeCount: number;
  highImpactCount: number;
};

/**
 * Mirrors ReadinessInsightsDto — the aggregate counts behind the Insights page's
 * Readiness and Audience & Roles tabs.
 */
export type ReadinessInsights = {
  activeInitiatives: number;
  atRisk: number;
  needsAttention: number;
  totalEnterpriseRoles: number;
  enterpriseRolesCovered: number;
  roleCoverage: EnterpriseRoleCoverageRecord[];
};

export const readinessInsightsQueryKey = ["initiatives", "readiness-insights"] as const;

export function useReadinessInsights() {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: readinessInsightsQueryKey,
    queryFn: () => apiFetch<ReadinessInsights>("/api/initiatives/insights"),
    enabled: isAuthenticated && isApiConfigured,
  });
}

/**
 * Creates an Initiative and refreshes the list so the new record shows up without a reload.
 */
export function useCreateInitiative() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateInitiativeRequest) =>
      apiFetch<Initiative>("/api/initiatives", {
        method: "POST",
        body: JSON.stringify(request),
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: initiativesQueryKey });
    },
  });
}

/**
 * Replaces an Initiative's fields and refreshes both the list and its own cached detail.
 * Same request shape as create — every field the New Initiative form collects is
 * editable, mirroring UpdateInitiativeRequestDto's inheritance from the create DTO.
 */
export function useUpdateInitiative(id: number | null) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateInitiativeRequest) =>
      apiFetch<Initiative>(`/api/initiatives/${id}`, {
        method: "PUT",
        body: JSON.stringify(request),
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: initiativesQueryKey });
      if (id !== null) {
        void queryClient.invalidateQueries({ queryKey: [...initiativesQueryKey, id] });
      }
    },
  });
}

/** Mirrors InitiativeDeletionImpactDto — what deleting an Initiative would also remove. */
export type InitiativeDeletionImpact = {
  contributionCount: number;
  taskCount: number;
  activityCount: number;
  teamMemberCount: number;
};

/**
 * Loads the counts a delete confirmation dialog names, for one Initiative at a time.
 * Only enabled while id is set, so this never fires until the user actually opens the
 * delete dialog for a specific card.
 */
export function useInitiativeDeletionImpact(id: number | null) {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: [...initiativesQueryKey, id, "deletion-impact"] as const,
    queryFn: () =>
      apiFetch<InitiativeDeletionImpact>(`/api/initiatives/${id}/deletion-impact`),
    enabled: isAuthenticated && isApiConfigured && id !== null,
  });
}

/**
 * Deletes an Initiative and everything cascading from it (Contributions, Tasks,
 * Activity, Team members — see useInitiativeDeletionImpact for the counts shown before
 * this is called).
 */
export function useDeleteInitiative() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) =>
      apiFetch<void>(`/api/initiatives/${id}`, { method: "DELETE" }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: initiativesQueryKey });
    },
  });
}
