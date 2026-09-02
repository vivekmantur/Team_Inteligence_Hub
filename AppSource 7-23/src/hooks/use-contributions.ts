import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiBaseUrl, apiFetch, getAccessToken, isApiConfigured } from "@/lib/api-client";
import { membersQueryKey } from "./use-initiative-members";
import { useAuth } from "./use-auth";

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

export type ContributionPriorityWire = "Low" | "Medium" | "High" | "Critical";

export type ContributionStatusWire = "Draft" | "Submitted";

export type RiskSeverityWire = "Low" | "Medium" | "High" | "Critical";

export type ContributionReuseTargetWire =
  | "ExecutiveBrief"
  | "LeadershipUpdate"
  | "Qbr"
  | "CustomerStoryLibrary"
  | "KnowledgeRepository"
  | "BestPractices"
  | "TeamNewsletter"
  | "VivaEngage";

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

export const reuseTargetToLabel = (wire: ContributionReuseTargetWire): string =>
  REUSE_TARGET_LABELS[wire] ?? wire;

export const reuseTargetToWire = (
  label: string,
): ContributionReuseTargetWire | undefined =>
  (Object.keys(REUSE_TARGET_LABELS) as ContributionReuseTargetWire[]).find(
    (key) => REUSE_TARGET_LABELS[key] === label,
  );

export const linkSourceToLabel = (wire: ContributionLinkSourceWire): string =>
  LINK_SOURCE_LABELS[wire] ?? wire;

export const linkSourceToWire = (label: string): ContributionLinkSourceWire =>
  (Object.keys(LINK_SOURCE_LABELS) as ContributionLinkSourceWire[]).find(
    (key) => LINK_SOURCE_LABELS[key] === label,
  ) ?? "ExternalUrl";

export const REUSE_TARGET_OPTIONS = Object.entries(REUSE_TARGET_LABELS).map(
  ([wire, label]) => ({ wire: wire as ContributionReuseTargetWire, label }),
);

export const LINK_SOURCE_OPTIONS = Object.entries(LINK_SOURCE_LABELS).map(
  ([wire, label]) => ({ wire: wire as ContributionLinkSourceWire, label }),
);

// ---------------------------------------------------------------------------
// Records, mirroring the response DTOs
// ---------------------------------------------------------------------------

export type ContributionContributorRecord = {
  id: number;
  userId: number;
  displayName: string;
  responsibilityArea: string | null;
  isPrimary: boolean;
  addedAt: string;
};

export type ContributionLinkRecord = {
  id: number;
  source: ContributionLinkSourceWire;
  url: string;
  label: string | null;
  description: string | null;
  createdAt: string;
};

export type ContributionAttachmentRecord = {
  id: number;
  contributionId: number;
  fileName: string;
  contentType: string;
  /** Bytes. Formatted for display by the client. */
  fileSize: number;
  createdAt: string;
};

export type ContributionMetricRecord = {
  metricName: string;
  unit: string | null;
  previousValue: number | null;
  currentValue: number | null;
  reportingPeriod: string | null;
};

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

export type ContributionAiPracticeRecord = {
  tool: string;
  useCase: string | null;
  prompt: string | null;
  timeSavedHoursPerWeek: number | null;
  recommendation: string | null;
};

export type ContributionCustomerStoryRecord = {
  customerName: string;
  summary: string | null;
  outcome: string | null;
  quote: string | null;
  businessValue: string | null;
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
};

// ---------------------------------------------------------------------------
// Requests
// ---------------------------------------------------------------------------

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
};

// ---------------------------------------------------------------------------
// Queries
// ---------------------------------------------------------------------------

export const contributionsQueryKey = (initiativeId: number) =>
  ["initiatives", initiativeId, "contributions"] as const;

export const contributionQueryKey = (contributionId: number) =>
  ["contributions", contributionId] as const;

export const contributionTagsQueryKey = ["contributions", "tags"] as const;

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
 * bearer token, so a plain `<a href>` cannot work — this is shared by both the "open to
 * preview" and "save to disk" paths below, which only differ in what they do with the
 * resulting object URL.
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
 * The API's Download action always sends `Content-Disposition: attachment` (ASP.NET
 * Core's `File(stream, contentType, fileDownloadName)` sets that whenever a download
 * name is passed), but that header only governs a direct HTTP navigation. Because the
 * bytes are fetched here and re-served from a local `blob:` URL, that header never
 * reaches the browser — rendering is decided purely by the Blob's MIME type, which is
 * what makes inline preview possible for the same response.
 *
 * File types the browser has no native viewer for (Word, Excel, PowerPoint) still fall
 * back to a save prompt — the same thing happens opening any Office file link outside
 * this app, since that is a browser limitation, not something this function controls.
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
