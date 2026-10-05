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

/** Writing tone as the API sends it. */
export type ContentToneWire =
  | "Confident"
  | "Inspirational"
  | "Analytical"
  | "StoryDriven"
  | "Playful";

/** Target audience as the API sends it. */
export type ContentAudienceWire =
  | "Leadership"
  | "Stakeholders"
  | "Customers"
  | "External"
  | "Team";

/** Output length as the API sends it. */
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

/**
 * Returns the wire value whose label matches, or `fallback` when none does. Each hook
 * module keeps its own copy of this helper and its label maps, so no hook file imports
 * another's internals.
 */
function toWire<T extends string>(labels: Record<T, string>, label: string, fallback: T): T {
  const match = (Object.keys(labels) as T[]).find((key) => labels[key] === label);
  return match ?? fallback;
}

/** Converts a format label to its wire value; falls back to "LinkedInPost". */
export const contentFormatToWire = (label: string): ContentFormatWire =>
  toWire(CONTENT_FORMAT_LABELS, label, "LinkedInPost");

/** Converts a format wire value to its display label, or returns the wire value if it has none. */
export const contentFormatToLabel = (wire: ContentFormatWire): string =>
  CONTENT_FORMAT_LABELS[wire] ?? wire;

/** Converts a tone label to its wire value; falls back to "Confident". */
export const contentToneToWire = (label: string): ContentToneWire =>
  toWire(CONTENT_TONE_LABELS, label, "Confident");

/** Converts a tone wire value to its display label, or returns the wire value if it has none. */
export const contentToneToLabel = (wire: ContentToneWire): string =>
  CONTENT_TONE_LABELS[wire] ?? wire;

/** Converts an audience label to its wire value; falls back to "Leadership". */
export const contentAudienceToWire = (label: string): ContentAudienceWire =>
  toWire(CONTENT_AUDIENCE_LABELS, label, "Leadership");

/** Converts an audience wire value to its display label, or returns the wire value if it has none. */
export const contentAudienceToLabel = (wire: ContentAudienceWire): string =>
  CONTENT_AUDIENCE_LABELS[wire] ?? wire;

/** Converts a length label to its wire value; falls back to "Medium". */
export const contentLengthToWire = (label: string): ContentLengthWire =>
  toWire(CONTENT_LENGTH_LABELS, label, "Medium");

/** Converts a length wire value to its display label, or returns the wire value if it has none. */
export const contentLengthToLabel = (wire: ContentLengthWire): string =>
  CONTENT_LENGTH_LABELS[wire] ?? wire;

/** Every format wire value, in label-map order. */
export const CONTENT_FORMATS = Object.keys(CONTENT_FORMAT_LABELS) as ContentFormatWire[];
/** Every tone wire value, in label-map order. */
export const CONTENT_TONES = Object.keys(CONTENT_TONE_LABELS) as ContentToneWire[];
/** Every audience wire value, in label-map order. */
export const CONTENT_AUDIENCES = Object.keys(CONTENT_AUDIENCE_LABELS) as ContentAudienceWire[];
/** Every length wire value, in label-map order. */
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
 * initiativeId is passed with each call instead of being bound when the hook is created,
 * so Retry replays a failed request against the Initiative it was first sent for, even
 * after the picker moves to another one.
 *
 * Errors reach the caller as apiFetch's ApiError, unchanged: a 502 from the provider or a
 * 404 for a missing Initiative arrives exactly as the server sent it.
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
