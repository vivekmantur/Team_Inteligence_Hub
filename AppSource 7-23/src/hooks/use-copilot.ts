import { useMutation } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";

/** Mirrors CopilotCitationDto from TeamIntelligenceHub.Application. */
export type CopilotCitation = {
  title: string | null;
  source: string | null;
  content: string;
};

/** Mirrors CopilotAnswerDto from TeamIntelligenceHub.Application. */
export type CopilotAnswer = {
  answer: string;
  citations: CopilotCitation[];
  /** Populated only when nothing relevant was found — pick one and resend instead. */
  suggestedQuestions: string[];
};

/**
 * Asks Copilot a question, grounded on whatever Azure AI Search retrieves for it.
 *
 * `POST /api/copilot/ask` embeds the question, searches the index, and runs the result
 * through the chat model server-side — this hook only sends the question and waits for
 * the finished answer, there is no streaming from the API yet.
 */
export function useAskCopilot() {
  return useMutation({
    mutationFn: (question: string) =>
      apiFetch<CopilotAnswer>("/api/copilot/ask", {
        method: "POST",
        body: JSON.stringify({ question }),
      }),
  });
}

export default useAskCopilot;
