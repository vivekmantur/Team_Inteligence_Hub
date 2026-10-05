// 1. Answer a question grounded on the indexed content

using System.Text;
using System.Text.RegularExpressions;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;
using TeamIntelligenceHub.Application.Interfaces.Services;

namespace TeamIntelligenceHub.Application.Services;

/// <summary>
/// Answers a question by grounding it on the indexed corpus: embed the question, pull
/// the nearest chunks, hand both to the chat model, and return its answer with the
/// chunks it was allowed to use as citations.
/// </summary>
public class CopilotService : ICopilotService
{
    /// <summary>
    /// The longest question, in characters, that <see cref="AskAsync"/> accepts.
    /// </summary>
    public const int QuestionMaxLength = 2000;

    /// <summary>
    /// What the model is told to answer with, verbatim, when the context can't answer the
    /// question. Checked for exactly rather than inferred from wording, so detecting a
    /// no-match never depends on guessing how a "sorry, I don't know" sentence reads.
    /// </summary>
    private const string NoRelevantDataMarker = "NO_RELEVANT_DATA";

    /// <summary>The rules the chat model answers under: ground on the context, and report conflicting values.</summary>
    private const string SystemPrompt =
        "You are the Copilot assistant for Team Intelligence Hub. Answer the user's " +
        "question using only the context provided below. If the context does not " +
        "contain enough information to answer, respond with exactly this line and " +
        $"nothing else: {NoRelevantDataMarker}\n\n" +
        "If the context contains more than one value for the same fact (e.g. two " +
        "sources reporting different figures for the same metric and period), do not " +
        "silently choose one. List every distinct value you find, attribute each to " +
        "its source, and state plainly that the sources disagree. Never average, " +
        "blend, or otherwise invent a reconciled number that no source actually " +
        "states. Only recommend which value to trust when the context itself gives a " +
        "concrete reason to (e.g. a stated methodology or explicit recency) — never " +
        "from assumption alone.";

    /// <summary>The answer shown ahead of the suggested questions when nothing relevant was found.</summary>
    private const string NoDataFoundMessage =
        "I couldn't find anything relevant to that in the indexed content. Try one of " +
        "these instead:";

    /// <summary>The rules for the follow-up call that proposes better phrasings of the question.</summary>
    private const string SuggestQuestionsSystemPrompt =
        "You help people ask better questions of a knowledge base about team " +
        "initiatives, contributions, and adoption metrics. A question could not be " +
        "answered from the indexed content. Propose exactly 3 alternative phrasings of " +
        "it that are clearer, more specific, or more likely to match indexed content. " +
        "Output only the 3 questions, one per line, with no numbering, bullets, or " +
        "other commentary.";

    private readonly IEmbeddingClient _embeddingClient;
    private readonly IVectorSearchClient _searchClient;
    private readonly IChatCompletionClient _chatClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotService"/> class.
    /// </summary>
    /// <param name="embeddingClient">The client that embeds the question.</param>
    /// <param name="searchClient">The client that finds the nearest indexed chunks.</param>
    /// <param name="chatClient">The chat model client that writes the answer and suggestions.</param>
    public CopilotService(
        IEmbeddingClient embeddingClient,
        IVectorSearchClient searchClient,
        IChatCompletionClient chatClient)
    {
        _embeddingClient = embeddingClient;
        _searchClient = searchClient;
        _chatClient = chatClient;
    }

    /// <inheritdoc />
    public async Task<CopilotAnswerDto> AskAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        question = ValidateQuestion(question);

        var queryVector = await _embeddingClient.EmbedAsync(question, cancellationToken);

        var chunks = await _searchClient.SearchAsync(
            queryVector, cancellationToken: cancellationToken);

        var userPrompt = BuildPrompt(question, chunks);

        var answer = await _chatClient.CompleteAsync(
            SystemPrompt, userPrompt, cancellationToken);

        if (IsNoRelevantData(answer))
        {
            return new CopilotAnswerDto
            {
                Answer = NoDataFoundMessage,
                Citations = new(),
                SuggestedQuestions = await SuggestBetterQuestionsAsync(
                    question, cancellationToken)
            };
        }

        return new CopilotAnswerDto
        {
            Answer = answer,
            Citations = chunks
                .Select(c => new CopilotCitationDto
                {
                    Title = c.Title,
                    Source = c.Source,
                    Content = c.Content
                })
                .ToList()
        };
    }

    /// <summary>Checks whether the model answered with the no-relevant-data marker.</summary>
    private static bool IsNoRelevantData(string answer) =>
        answer.Trim().Equals(NoRelevantDataMarker, StringComparison.Ordinal);

    /// <summary>
    /// A second, separate model call rather than folding this into the main answer — the
    /// main prompt is grounded strictly on retrieved chunks, but rephrasing a question
    /// well doesn't need that context and asking for both at once risks the model
    /// blending an attempted answer in with the suggestions.
    /// </summary>
    private async Task<List<string>> SuggestBetterQuestionsAsync(
        string question, CancellationToken cancellationToken)
    {
        var raw = await _chatClient.CompleteAsync(
            SuggestQuestionsSystemPrompt,
            $"Original question: {question}",
            cancellationToken);

        return raw
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(StripListMarker)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Take(3)
            .ToList();
    }

    /// <summary>
    /// Strips a leading "1.", "-", "*", etc., in case the model adds one anyway. Anchored
    /// to at most two digits followed by punctuation, rather than trimming any leading
    /// digit character, so a question that legitimately starts with a number (e.g. "2026
    /// adoption rate?") is not mangled.
    /// </summary>
    private static readonly Regex ListMarkerPattern = new(@"^\s*(?:[-*•]|\d{1,2}[\.\)])\s*");

    private static string StripListMarker(string line) =>
        ListMarkerPattern.Replace(line, string.Empty).Trim();

    /// <summary>Trims the question and rejects it when blank or longer than <see cref="QuestionMaxLength"/>.</summary>
    private static string ValidateQuestion(string? question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ValidationException("A question is required.");
        }

        question = question.Trim();

        if (question.Length > QuestionMaxLength)
        {
            throw new ValidationException(
                $"Question cannot exceed {QuestionMaxLength} characters.");
        }

        return question;
    }

    /// <summary>
    /// Lays the retrieved chunks ahead of the question so the model reads them as
    /// context it is expected to ground on, not as part of what the user asked.
    /// </summary>
    private static string BuildPrompt(string question, IReadOnlyList<RetrievedChunk> chunks)
    {
        if (chunks.Count == 0)
        {
            return $"Context: (nothing relevant was found)\n\nQuestion: {question}";
        }

        var sb = new StringBuilder("Context:\n");

        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];

            sb.Append($"[{i + 1}] ");

            if (!string.IsNullOrWhiteSpace(chunk.Title))
            {
                sb.Append($"{chunk.Title}: ");
            }

            sb.Append(chunk.Content.Trim()).Append("\n\n");
        }

        sb.Append($"Question: {question}");

        return sb.ToString();
    }
}
