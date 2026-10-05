import { useMemo } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiBaseUrl, apiFetch, getAccessToken, isApiConfigured } from "@/lib/api-client";
import { membersQueryKey } from "./use-initiative-members";
import { useAuth } from "./use-auth";
import { useBackendUser } from "./use-backend-user";

/**
 * The API speaks enum names; the wizard shows display labels. Translating between them is
 * this module's job so no component has to know the wire format.
 */
export type ContributionTypeWire =
  | "ProgressUpdate"
  | "Deliverable"
  | "CustomerStory"
  | "BusinessMetric"
  | "Risk"
  | "Decision"
  | "AiBestPractice"
  | "Testimonial"
  | "SupportingAsset"
  | "SupportNeeded"
  | "Other";

/** Contribution priority as the API sends it. */
export type ContributionPriorityWire = "Low" | "Medium" | "High" | "Critical";

/** Whether a contribution is still a draft or has been submitted. */
export type ContributionStatusWire = "Draft" | "Submitted";

/** Severity of a risk contribution. */
export type RiskSeverityWire = "Low" | "Medium" | "High" | "Critical";

/** Who a testimonial is addressed to. */
export type TestimonialAudienceWire = "Leadership" | "Stakeholder" | "Customer" | "Team";

/** Overall tone of a testimonial. */
export type TestimonialSentimentWire = "Positive" | "Neutral" | "Constructive";

/** Where a contribution can be reused, such as an executive brief or a QBR. */
export type ContributionReuseTargetWire =
  | "ExecutiveBrief"
  | "LeadershipUpdate"
  | "Qbr"
  | "CustomerStoryLibrary"
  | "KnowledgeRepository"
  | "BestPractices"
  | "TeamNewsletter"
  | "VivaEngage";

/** The system a contribution link points into. */
export type ContributionLinkSourceWire =
  | "SharePoint"
  | "Teams"
  | "Loop"
  | "OneDrive"
  | "ExternalUrl";

const REUSE_TARGET_LABELS: Record<ContributionReuseTargetWire, string> = {
  ExecutiveBrief: "Executive Brief",
  LeadershipUpdate: "Leadership Update",
  Qbr: "QBR",
  CustomerStoryLibrary: "Customer Story Library",
  KnowledgeRepository: "Knowledge Repository",
  BestPractices: "Best Practices",
  TeamNewsletter: "Team Newsletter",
  VivaEngage: "Viva Engage",
};

const LINK_SOURCE_LABELS: Record<ContributionLinkSourceWire, string> = {
  SharePoint: "SharePoint",
  Teams: "Teams",
  Loop: "Loop",
  OneDrive: "OneDrive",
  ExternalUrl: "External URL",
};

/** Converts a reuse target wire value to its display label, or returns the wire value if it has none. */
export const reuseTargetToLabel = (wire: ContributionReuseTargetWire): string =>
  REUSE_TARGET_LABELS[wire] ?? wire;

/** Converts a reuse target label to its wire value, or returns undefined when the label is unknown. */
export const reuseTargetToWire = (
  label: string,
): ContributionReuseTargetWire | undefined =>
  (Object.keys(REUSE_TARGET_LABELS) as ContributionReuseTargetWire[]).find(
    (key) => REUSE_TARGET_LABELS[key] === label,
  );

/** Converts a link source wire value to its display label, or returns the wire value if it has none. */
export const linkSourceToLabel = (wire: ContributionLinkSourceWire): string =>
  LINK_SOURCE_LABELS[wire] ?? wire;

/** Converts a link source label to its wire value; falls back to "ExternalUrl". */
export const linkSourceToWire = (label: string): ContributionLinkSourceWire =>
  (Object.keys(LINK_SOURCE_LABELS) as ContributionLinkSourceWire[]).find(
    (key) => LINK_SOURCE_LABELS[key] === label,
  ) ?? "ExternalUrl";

/** Every reuse target as a `{ wire, label }` pair, in label-map order, for pickers. */
export const REUSE_TARGET_OPTIONS = Object.entries(REUSE_TARGET_LABELS).map(
  ([wire, label]) => ({ wire: wire as ContributionReuseTargetWire, label }),
);

/** Every link source as a `{ wire, label }` pair, in label-map order, for pickers. */
export const LINK_SOURCE_OPTIONS = Object.entries(LINK_SOURCE_LABELS).map(
  ([wire, label]) => ({ wire: wire as ContributionLinkSourceWire, label }),
);

// ---------------------------------------------------------------------------
// Records, mirroring the response DTOs
// ---------------------------------------------------------------------------

/** Mirrors the contributor entry on ContributionResponseDto. */
export type ContributionContributorRecord = {
  id: number;
  userId: number;
  displayName: string;
  responsibilityArea: string | null;
  isPrimary: boolean;
  addedAt: string;
};

/** Mirrors the link entry on ContributionResponseDto. */
export type ContributionLinkRecord = {
  id: number;
  source: ContributionLinkSourceWire;
  url: string;
  label: string | null;
  description: string | null;
  createdAt: string;
};

/** Mirrors the attachment entry on ContributionResponseDto. */
export type ContributionAttachmentRecord = {
  id: number;
  contributionId: number;
  fileName: string;
  contentType: string;
  /** Bytes. Formatted for display by the client. */
  fileSize: number;
  createdAt: string;
};

/** Business metric details, present when the contribution includes a metric. */
export type ContributionMetricRecord = {
  metricName: string;
  unit: string | null;
  previousValue: number | null;
  currentValue: number | null;
  reportingPeriod: string | null;
};

/** Risk details, present when the contribution includes a risk. */
export type ContributionRiskRecord = {
  description: string;
  severity: RiskSeverityWire;
  businessImpact: string | null;
  mitigation: string | null;
  supportNeeded: string | null;
  ownerUserId: number | null;
  ownerDisplayName: string | null;
  targetResolutionDate: string | null;
};

/** AI best-practice details, present when the contribution includes one. */
export type ContributionAiPracticeRecord = {
  tool: string;
  useCase: string | null;
  prompt: string | null;
  timeSavedHoursPerWeek: number | null;
  recommendation: string | null;
};

/** Customer story details, present when the contribution includes one. */
export type ContributionCustomerStoryRecord = {
  customerName: string;
  summary: string | null;
  outcome: string | null;
  quote: string | null;
  businessValue: string | null;
};

/** Testimonial details, present when the contribution includes one. */
export type ContributionTestimonialRecord = {
  quote: string;
  speakerName: string;
  speakerRole: string | null;
  audience: TestimonialAudienceWire;
  sentiment: TestimonialSentimentWire;
};

/** Mirrors ContributionResponseDto. */
export type ContributionRecord = {
  id: number;
  initiativeId: number;
  initiativeName: string;
  workstream: string;
  submittedByUserId: number;
  submittedByDisplayName: string;
  title: string;
  description: string;
  keyTakeaway: string | null;
  priority: ContributionPriorityWire;
  status: ContributionStatusWire;
  types: ContributionTypeWire[];
  tags: string[];
  reuseTargets: ContributionReuseTargetWire[];
  submittedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
  contributors: ContributionContributorRecord[];
  links: ContributionLinkRecord[];
  attachments: ContributionAttachmentRecord[];
  metric: ContributionMetricRecord | null;
  risk: ContributionRiskRecord | null;
  aiPractice: ContributionAiPracticeRecord | null;
  customerStory: ContributionCustomerStoryRecord | null;
  testimonial: ContributionTestimonialRecord | null;
};

// ---------------------------------------------------------------------------
// Requests
// ---------------------------------------------------------------------------

/** Body for creating or editing a contribution, including its optional typed sections. */
export type SaveContributionRequest = {
  title: string;
  description: string;
  keyTakeaway?: string | null;
  priority?: ContributionPriorityWire;
  status?: ContributionStatusWire;
  types: ContributionTypeWire[];
  tags?: string[];
  reuseTargets?: ContributionReuseTargetWire[];
  contributors?: {
    userId: number;
    responsibilityArea?: string | null;
    isPrimary: boolean;
  }[];
  links?: {
    source: ContributionLinkSourceWire;
    url: string;
    label?: string | null;
    description?: string | null;
  }[];
  metric?: {
    metricName: string;
    unit?: string | null;
    previousValue?: number | null;
    currentValue?: number | null;
    reportingPeriod?: string | null;
  } | null;
  risk?: {
    description: string;
    severity?: RiskSeverityWire;
    businessImpact?: string | null;
    mitigation?: string | null;
    supportNeeded?: string | null;
    ownerUserId?: number | null;
    /** `yyyy-MM-dd`, which binds to DateOnly. */
    targetResolutionDate?: string | null;
  } | null;
  aiPractice?: {
    tool: string;
    useCase?: string | null;
    prompt?: string | null;
    timeSavedHoursPerWeek?: number | null;
    recommendation?: string | null;
  } | null;
  customerStory?: {
    customerName: string;
    summary?: string | null;
    outcome?: string | null;
    quote?: string | null;
    businessValue?: string | null;
  } | null;
  testimonial?: {
    quote: string;
    speakerName: string;
    speakerRole?: string | null;
    audience?: TestimonialAudienceWire;
    sentiment?: TestimonialSentimentWire;
  } | null;
};

/** Mirrors CustomerStoryCardDto — the slim shape the Stories & Evidence grid reads. */
export type CustomerStoryCardRecord = {
  id: number;
  initiativeId: number;
  initiativeName: string;
  submittedByUserId: number;
  title: string;
  keyTakeaway: string | null;
  submittedAt: string | null;
  customerName: string;
  summary: string | null;
  outcome: string | null;
  quote: string | null;
  businessValue: string | null;
};

/** Mirrors TestimonialCardDto — the slim shape the Stories & Evidence grid reads. */
export type TestimonialCardRecord = {
  id: number;
  initiativeId: number;
  initiativeName: string;
  submittedByUserId: number;
  submittedAt: string | null;
  quote: string;
  speakerName: string;
  speakerRole: string | null;
  audience: TestimonialAudienceWire;
  sentiment: TestimonialSentimentWire;
};

/**
 * Mirrors DocumentCustomerStoryCardDto — a customer story extracted from a Contribution
 * attachment's document content, for the Stories & Evidence page's "Extracted from
 * documents" section. Carries a source file name instead of a contributor-written title.
 */
export type DocumentCustomerStoryCardRecord = {
  id: number;
  contributionId: number;
  initiativeId: number;
  initiativeName: string;
  sourceFileName: string;
  customerName: string | null;
  summary: string | null;
  outcome: string | null;
  quote: string | null;
  businessValue: string | null;
};

/**
 * Mirrors DocumentTestimonialCardDto — a testimonial extracted from a Contribution
 * attachment's document content. See DocumentCustomerStoryCardRecord for why this is
 * separate from TestimonialCardRecord.
 */
export type DocumentTestimonialCardRecord = {
  id: number;
  contributionId: number;
  initiativeId: number;
  initiativeName: string;
  sourceFileName: string;
  quote: string | null;
  speakerName: string | null;
  speakerRole: string | null;
  audience: TestimonialAudienceWire | null;
  sentiment: TestimonialSentimentWire | null;
};

// ---------------------------------------------------------------------------
// Queries
// ---------------------------------------------------------------------------

/** Builds the React Query cache key for one Initiative's contribution list. */
export const contributionsQueryKey = (initiativeId: number) =>
  ["initiatives", initiativeId, "contributions"] as const;

/** Builds the React Query cache key for a single contribution. */
export const contributionQueryKey = (contributionId: number) =>
  ["contributions", contributionId] as const;

/** React Query cache key prefix for tag typeahead results; the search text is appended. */
export const contributionTagsQueryKey = ["contributions", "tags"] as const;

/** React Query cache key for the company-wide customer story cards. */
export const customerStoriesQueryKey = ["contributions", "customer-stories"] as const;

/** React Query cache key for the company-wide testimonial cards. */
export const testimonialsQueryKey = ["contributions", "testimonials"] as const;

/** React Query cache key for customer stories extracted from attachments. */
export const documentCustomerStoriesQueryKey =
  ["contributions", "document-customer-stories"] as const;

/** React Query cache key for testimonials extracted from attachments. */
export const documentTestimonialsQueryKey =
  ["contributions", "document-testimonials"] as const;

/**
 * Loads one Initiative's contributions. Disabled until the user is signed in, the API is
 * configured, and an initiativeId is set.
 */
export function useInitiativeContributions(initiativeId: number | null) {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: contributionsQueryKey(initiativeId ?? 0),
    queryFn: () =>
      apiFetch<ContributionRecord[]>(
        `/api/initiatives/${initiativeId}/contributions`,
      ),
    enabled: isAuthenticated && isApiConfigured && initiativeId !== null,
  });
}

/**
 * Loads one contribution by id. Disabled until the user is signed in, the API is
 * configured, and a contributionId is set.
 */
export function useContribution(contributionId: number | null) {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: contributionQueryKey(contributionId ?? 0),
    queryFn: () =>
      apiFetch<ContributionRecord>(`/api/contributions/${contributionId}`),
    enabled: isAuthenticated && isApiConfigured && contributionId !== null,
  });
}

/**
 * Tags already in use, for the typeahead in step 3.
 *
 * Offering what exists is what stops "Copilot", "copilot", and "co-pilot" becoming three
 * tags for one idea. Tags live in a JSON column with no unique index, so this is the
 * only thing keeping the vocabulary coherent.
 */
export function useContributionTags(search?: string) {
  const { isAuthenticated } = useAuth();
  const query = search?.trim() ?? "";

  return useQuery({
    queryKey: [...contributionTagsQueryKey, query] as const,
    queryFn: () =>
      apiFetch<string[]>(
        `/api/contributions/tags${query ? `?q=${encodeURIComponent(query)}` : ""}`,
      ),
    enabled: isAuthenticated && isApiConfigured,
    staleTime: 5 * 60 * 1000,
  });
}

/**
 * Every submitted Customer Story, across all Initiatives, for the Stories & Evidence
 * page's Customer Zero grid — company-wide, unlike useInitiativeContributions.
 */
export function useCustomerStories() {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: customerStoriesQueryKey,
    queryFn: () =>
      apiFetch<CustomerStoryCardRecord[]>("/api/contributions/customer-stories"),
    enabled: isAuthenticated && isApiConfigured,
  });
}

/**
 * Every submitted Testimonial, across all Initiatives, for the Stories & Evidence page's
 * Testimonial grid — company-wide, same as useCustomerStories.
 */
export function useTestimonials() {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: testimonialsQueryKey,
    queryFn: () => apiFetch<TestimonialCardRecord[]>("/api/contributions/testimonials"),
    enabled: isAuthenticated && isApiConfigured,
  });
}

/**
 * Every customer story extracted from a Contribution attachment's document content, for
 * the Stories & Evidence page's "Extracted from documents" section.
 */
export function useDocumentCustomerStories() {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: documentCustomerStoriesQueryKey,
    queryFn: () =>
      apiFetch<DocumentCustomerStoryCardRecord[]>(
        "/api/contributions/document-customer-stories",
      ),
    enabled: isAuthenticated && isApiConfigured,
  });
}

/**
 * Every testimonial extracted from a Contribution attachment's document content, same
 * section as useDocumentCustomerStories.
 */
export function useDocumentTestimonials() {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: documentTestimonialsQueryKey,
    queryFn: () =>
      apiFetch<DocumentTestimonialCardRecord[]>(
        "/api/contributions/document-testimonials",
      ),
    enabled: isAuthenticated && isApiConfigured,
  });
}

/** True when the ISO timestamp falls in the current calendar month, in local time. */
function isInCurrentMonth(iso: string | null): boolean {
  if (!iso) return false;

  const date = new Date(iso);
  const now = new Date();

  return date.getFullYear() === now.getFullYear() && date.getMonth() === now.getMonth();
}

/**
 * How many Customer Story / Testimonial contributions the signed-in user has submitted
 * this calendar month — the Home page's "Submitted evidence" highlight.
 *
 * Reuses the same company-wide reads Stories & Evidence already makes rather than a
 * dedicated endpoint, filtering client-side to this user and this month.
 */
export function useMySubmittedEvidenceThisMonthCount() {
  const { data: backendUser } = useBackendUser();
  const { data: customerStories = [], isLoading: isLoadingCustomerStories } =
    useCustomerStories();
  const { data: testimonials = [], isLoading: isLoadingTestimonials } = useTestimonials();

  const count = useMemo(() => {
    if (!backendUser) return 0;

    const mine = (items: { submittedByUserId: number; submittedAt: string | null }[]) =>
      items.filter(
        (item) => item.submittedByUserId === backendUser.id && isInCurrentMonth(item.submittedAt),
      ).length;

    return mine(customerStories) + mine(testimonials);
  }, [backendUser, customerStories, testimonials]);

  return { count, isLoading: isLoadingCustomerStories || isLoadingTestimonials };
}

// ---------------------------------------------------------------------------
// Mutations
// ---------------------------------------------------------------------------

/**
 * Refreshes the contribution list and the team after a write.
 *
 * The team list matters because crediting somebody enrols them on the Initiative, so one
 * contribution can add a member as a side effect. The tag vocabulary is invalidated too,
 * since a new tag has to reach the typeahead.
 */
function useContributionRefresh(initiativeId: number | null) {
  const queryClient = useQueryClient();

  return (contributionId?: number) => {
    void queryClient.invalidateQueries({
      queryKey: contributionsQueryKey(initiativeId ?? 0),
    });
    void queryClient.invalidateQueries({
      queryKey: membersQueryKey(initiativeId ?? 0),
    });
    void queryClient.invalidateQueries({ queryKey: contributionTagsQueryKey });

    if (contributionId !== undefined) {
      void queryClient.invalidateQueries({
        queryKey: contributionQueryKey(contributionId),
      });
    }
  };
}

/**
 * Creates a contribution on the Initiative, then refreshes its contributions, team, tags,
 * and the new contribution's own cache entry.
 */
export function useCreateContribution(initiativeId: number | null) {
  const refresh = useContributionRefresh(initiativeId);

  return useMutation({
    mutationFn: (request: SaveContributionRequest) =>
      apiFetch<ContributionRecord>(
        `/api/initiatives/${initiativeId}/contributions`,
        { method: "POST", body: JSON.stringify(request) },
      ),
    onSuccess: (created) => refresh(created.id),
  });
}

/**
 * Replaces a contribution's fields (PUT), then refreshes the Initiative's contributions,
 * team, tags, and that contribution's cache entry.
 */
export function useUpdateContribution(initiativeId: number | null) {
  const refresh = useContributionRefresh(initiativeId);

  return useMutation({
    mutationFn: ({
      contributionId,
      ...request
    }: SaveContributionRequest & { contributionId: number }) =>
      apiFetch<ContributionRecord>(`/api/contributions/${contributionId}`, {
        method: "PUT",
        body: JSON.stringify(request),
      }),
    onSuccess: (updated) => refresh(updated.id),
  });
}

/** Deletes a contribution by id, then refreshes the Initiative's contributions, team, and tags. */
export function useDeleteContribution(initiativeId: number | null) {
  const refresh = useContributionRefresh(initiativeId);

  return useMutation({
    mutationFn: (contributionId: number) =>
      apiFetch<void>(`/api/contributions/${contributionId}`, { method: "DELETE" }),
    onSuccess: () => refresh(),
  });
}

/**
 * Uploads one file against a contribution.
 *
 * Sent as multipart rather than through apiFetch, which sets a JSON content type. The
 * browser has to set its own boundary here, so the token is attached by hand.
 *
 * The contribution has to exist first: its id is the foreign key the row hangs off. That
 * is what the wizard's Save draft step is for.
 */
export function useUploadContributionAttachment(initiativeId: number | null) {
  const refresh = useContributionRefresh(initiativeId);

  return useMutation({
    mutationFn: async ({
      contributionId,
      file,
    }: {
      contributionId: number;
      file: File;
    }) => {
      const token = await getAccessToken();
      const form = new FormData();
      form.append("file", file);

      const response = await fetch(
        `${apiBaseUrl}/api/contributions/${contributionId}/attachments`,
        {
          method: "POST",
          headers: { Authorization: `Bearer ${token}` },
          body: form,
        },
      );

      if (!response.ok) {
        const body = await response.text();
        let message = `${response.status} ${response.statusText}`;
        try {
          const parsed = JSON.parse(body);
          if (parsed?.message) message = String(parsed.message);
        } catch {
          // Non-JSON error body — keep the status line.
        }
        throw new Error(message);
      }

      return (await response.json()) as ContributionAttachmentRecord;
    },
    onSuccess: (created) => refresh(created.contributionId),
  });
}

/**
 * Deletes one attachment from a contribution, then refreshes the Initiative's
 * contributions, team, tags, and that contribution's cache entry.
 */
export function useDeleteContributionAttachment(initiativeId: number | null) {
  const refresh = useContributionRefresh(initiativeId);

  return useMutation({
    mutationFn: ({
      contributionId,
      attachmentId,
    }: {
      contributionId: number;
      attachmentId: number;
    }) =>
      apiFetch<void>(
        `/api/contributions/${contributionId}/attachments/${attachmentId}`,
        { method: "DELETE" },
      ),
    onSuccess: (_result, variables) => refresh(variables.contributionId),
  });
}

/**
 * Fetches an attachment's bytes. The container is private and the endpoint requires a
 * bearer token, so a plain `<a href>` cannot work. Both the preview and download helpers
 * below use this and differ only in what they do with the resulting object URL.
 */
async function fetchAttachmentBlob(
  contributionId: number,
  attachment: ContributionAttachmentRecord,
): Promise<Blob> {
  const token = await getAccessToken();

  const response = await fetch(
    `${apiBaseUrl}/api/contributions/${contributionId}/attachments/${attachment.id}/download`,
    { headers: { Authorization: `Bearer ${token}` } },
  );

  if (!response.ok) {
    throw new Error(`Could not open ${attachment.fileName}.`);
  }

  return response.blob();
}

/**
 * Fetches an attachment and opens it in a new tab so the browser renders it inline
 * (PDF, images) instead of forcing a save dialog.
 *
 * The API always sends `Content-Disposition: attachment`, but that header only applies
 * to a direct navigation. Serving the fetched bytes from a local `blob:` URL leaves the
 * Blob's MIME type to decide how the browser renders them, which allows inline preview.
 *
 * Types without a native browser viewer (Word, Excel, PowerPoint) still prompt to save.
 * That is a browser limitation, the same as opening any Office link outside this app.
 */
export async function openContributionAttachment(
  contributionId: number,
  attachment: ContributionAttachmentRecord,
): Promise<void> {
  const blob = await fetchAttachmentBlob(contributionId, attachment);
  const url = URL.createObjectURL(blob);

  const win = window.open(url, "_blank", "noopener,noreferrer");
  if (!win) {
    // Popup blocked — fall back to a same-tab navigation so the file still opens.
    window.location.assign(url);
  }

  // Revoked on a delay rather than immediately, so the new tab has time to actually
  // load the blob before its URL stops resolving.
  setTimeout(() => URL.revokeObjectURL(url), 60_000);
}

/**
 * Fetches an attachment and hands it to the browser as an explicit download, regardless
 * of whether the browser could otherwise render it inline.
 */
export async function downloadContributionAttachment(
  contributionId: number,
  attachment: ContributionAttachmentRecord,
): Promise<void> {
  const blob = await fetchAttachmentBlob(contributionId, attachment);
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");

  anchor.href = url;
  anchor.download = attachment.fileName;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();

  URL.revokeObjectURL(url);
}
