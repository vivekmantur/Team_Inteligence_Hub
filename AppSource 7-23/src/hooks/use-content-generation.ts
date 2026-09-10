import { useMutation } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";

/**
 * The API speaks enum names; Content Studio's UI shows its own labels. Translating
 * between them is this module's job so content-studio.tsx never has to know the wire
 * format — same split as ContributionTypeWire/EnterpriseRoleWire elsewhere in this app.
 */
export type ContentFormatWire =
  | "LinkedInPost"
  | "VivaEngagePost"
  | "Newsletter"
  | "ExecutiveSummary"
  | "QbrSlide"
  | "Blog"
  | "CaseStudy";

export type ContentToneWire =
  | "Confident"
  | "Inspirational"
  | "Analytical"
  | "StoryDriven"
  | "Playful";

export type ContentAudienceWire =
  | "Leadership"
  | "Stakeholders"
  | "Customers"
  | "External"
  | "Team";

export type ContentLengthWire = "Short" | "Medium" | "Long";

const CONTENT_FORMAT_LABELS: Record<ContentFormatWire, string> = {
  LinkedInPost: "LinkedIn Post",
  VivaEngagePost: "Viva Engage Post",
  Newsletter: "Newsletter",
  ExecutiveSummary: "Executive Summary",
  QbrSlide: "QBR Slide",
  Blog: "Blog",
  CaseStudy: "Case Study",
};

const CONTENT_TONE_LABELS: Record<ContentToneWire, string> = {
  Confident: "Confident",
  Inspirational: "Inspirational",
  Analytical: "Analytical",
  StoryDriven: "Story-driven",
  Playful: "Playful",
};

const CONTENT_AUDIENCE_LABELS: Record<ContentAudienceWire, string> = {
  Leadership: "Leadership",
  Stakeholders: "Stakeholders",
  Customers: "Customers",
  External: "External",
  Team: "Team",
};

const CONTENT_LENGTH_LABELS: Record<ContentLengthWire, string> = {
  Short: "Short",
  Medium: "Medium",
  Long: "Long",
};

/** Same shape as use-initiatives-api.ts's local toWire helper — kept file-local rather than shared, matching how use-contributions.ts keeps its own label maps rather than importing another hook file's. */
function toWire<T extends string>(labels: Record<T, string>, label: string, fallback: T): T {
  const match = (Object.keys(labels) as T[]).find((key) => labels[key] === label);
  return match ?? fallback;
}

export const contentFormatToWire = (label: string): ContentFormatWire =>
  toWire(CONTENT_FORMAT_LABELS, label, "LinkedInPost");

export const contentFormatToLabel = (wire: ContentFormatWire): string =>
  CONTENT_FORMAT_LABELS[wire] ?? wire;

export const contentToneToWire = (label: string): ContentToneWire =>
  toWire(CONTENT_TONE_LABELS, label, "Confident");

export const contentToneToLabel = (wire: ContentToneWire): string =>
  CONTENT_TONE_LABELS[wire] ?? wire;

export const contentAudienceToWire = (label: string): ContentAudienceWire =>
  toWire(CONTENT_AUDIENCE_LABELS, label, "Leadership");

export const contentAudienceToLabel = (wire: ContentAudienceWire): string =>
  CONTENT_AUDIENCE_LABELS[wire] ?? wire;

export const contentLengthToWire = (label: string): ContentLengthWire =>
  toWire(CONTENT_LENGTH_LABELS, label, "Medium");

export const contentLengthToLabel = (wire: ContentLengthWire): string =>
  CONTENT_LENGTH_LABELS[wire] ?? wire;

export const CONTENT_FORMATS = Object.keys(CONTENT_FORMAT_LABELS) as ContentFormatWire[];
export const CONTENT_TONES = Object.keys(CONTENT_TONE_LABELS) as ContentToneWire[];
export const CONTENT_AUDIENCES = Object.keys(CONTENT_AUDIENCE_LABELS) as ContentAudienceWire[];
export const CONTENT_LENGTHS = Object.keys(CONTENT_LENGTH_LABELS) as ContentLengthWire[];

// ---------------------------------------------------------------------------
// Request / response
// ---------------------------------------------------------------------------

/** Longest the free-text instructions field may be — matches ContentGenerationRequestDto.InstructionsMaxLength. */
export const CONTENT_INSTRUCTIONS_MAX_LENGTH = 1000;

/** One prior successful instruction/output pair — mirrors ContentGenerationTurnDto. */
export type ContentGenerationTurn = {
  instruction: string;
  output: string;
};

/**
 * Mirrors ContentGenerationRequestDto. Carries only the four generation selections plus
 * optional free-text instructions — never a Contribution, Metric, Risk, Customer Story,
 * or Attachment id, a prompt template, or any Azure OpenAI configuration. The backend
 * loads and scopes everything else itself once the Initiative is confirmed to exist.
 */
export type ContentGenerationRequest = {
  format: ContentFormatWire;
  tone: ContentToneWire;
  audience: ContentAudienceWire;
  length: ContentLengthWire;
  /** What to emphasize, a style note, a constraint — attached to the prompt as the caller's own guidance. */
  instructions?: string;
  /**
   * Prior successful instruction/output pairs from this same Content Studio session
   * (same Initiative and format), oldest first. Omitted entirely for the first
   * generation in a session — see ContentGenerationRequestDto.PreviousTurns.
   */
  previousTurns?: ContentGenerationTurn[];
};

/** Mirrors ContentGenerationResponseDto. */
export type ContentGenerationResult = {
  content: string;
  format: ContentFormatWire;
  initiativeId: number;
  usedDocumentRetrieval: boolean;
  sourceCount: number;
  generationId: string | null;
};

// ---------------------------------------------------------------------------
// Mutation
// ---------------------------------------------------------------------------

/**
 * Generates one piece of Content Studio output for an Initiative.
 *
 * initiativeId travels with each call rather than being bound once — unlike
 * useCreateContribution's split, Retry has to replay a failed request against the exact
 * Initiative it was originally submitted for, even if the picker has since moved to a
 * different one, so the Initiative can't be fixed at hook-creation time here. Same
 * per-call-id shape as useUpdateContribution's mutationFn.
 *
 * Failures surface as apiFetch's own ApiError (status + server message), unmodified —
 * this hook adds no error handling of its own, so a 502 from a provider failure or a 404
 * from a missing Initiative reach the caller exactly as the server sent them.
 */
export function useGenerateContent() {
  return useMutation({
    mutationFn: ({
      initiativeId,
      ...request
    }: ContentGenerationRequest & { initiativeId: number }) =>
      apiFetch<ContentGenerationResult>(
        `/api/initiatives/${initiativeId}/content-generation`,
        { method: "POST", body: JSON.stringify(request) },
      ),
  });
}
