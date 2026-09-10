using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Application.Interfaces.Services;
using TeamIntelligenceHub.Application.Services.ContentGeneration;
using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Application.Services;

/// <summary>
/// Loads an Initiative's structured data, builds the format-specific prompt, and asks
/// Azure OpenAI to generate one piece of Content Studio output.
/// </summary>
/// <remarks>
/// Structured data only, for every format — Blog and CaseStudy included. No
/// IVectorSearchClient or IEmbeddingClient dependency exists on this service at all:
/// nothing here can retrieve an attachment. Initiative-scoped RAG for Blog/CaseStudy is a
/// separate follow-up feature, not started here.
/// </remarks>
public class ContentGenerationService : IContentGenerationService
{
    private readonly IInitiativeRepository _initiativeRepository;
    private readonly IContributionRepository _contributionRepository;
    private readonly IChatCompletionClient _chatClient;

    public ContentGenerationService(
        IInitiativeRepository initiativeRepository,
        IContributionRepository contributionRepository,
        IChatCompletionClient chatClient)
    {
        _initiativeRepository = initiativeRepository;
        _contributionRepository = contributionRepository;
        _chatClient = chatClient;
    }

    public async Task<ContentGenerationResponseDto> GenerateAsync(
        int initiativeId,
        ContentGenerationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var (format, tone, audience, length) = RequireSelections(request);

        var initiative = await _initiativeRepository.GetByIdAsync(initiativeId)
            ?? throw new NotFoundException($"Initiative {initiativeId} does not exist.");

        var contributions = await _contributionRepository.GetByInitiativeIdAsync(initiativeId);

        var context = ContentGenerationContextBuilder.Build(format, initiative, contributions);
        var instructions = Clean(request.Instructions);
        var previousTurns = MapPreviousTurns(request.PreviousTurns);

        var prompt = ContentPromptBuilder.Build(
            format, context, tone, audience, length, instructions, previousTurns);

        var content = await _chatClient.CompleteAsync(
            prompt.SystemPrompt, prompt.UserPrompt, cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            // Empty or malformed model output is a provider failure, not a successful
            // generation with nothing in it — the caller sees the same retryable error
            // as any other chat-completion failure.
            throw new CopilotException("Content generation returned an empty response.");
        }

        return new ContentGenerationResponseDto
        {
            Content = content,
            Format = format,
            InitiativeId = initiativeId,
            UsedDocumentRetrieval = false,
            SourceCount = 0,
            GenerationId = null
        };
    }

    /// <summary>
    /// [Required] on the request DTO already rejects a missing value for a caller coming
    /// through the controller; this covers the service being called directly (e.g. from
    /// tests) without one, matching InitiativeService/ContributionService's own re-check
    /// of their "required" request fields.
    /// </summary>
    private static (
        ContentFormat Format, ContentTone Tone, ContentAudience Audience, ContentLength Length)
        RequireSelections(ContentGenerationRequestDto request)
    {
        var format = request.Format ?? throw new ValidationException("Format is required.");
        var tone = request.Tone ?? throw new ValidationException("Tone is required.");
        var audience = request.Audience ?? throw new ValidationException("Audience is required.");
        var length = request.Length ?? throw new ValidationException("Length is required.");

        return (format, tone, audience, length);
    }

    /// <summary>Trims and turns whitespace-only into null, matching ContributionService's Clean.</summary>
    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();

        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary>
    /// Maps the request's validated PreviousTurns DTOs to the prompt builder's own
    /// ContentGenerationTurn type. ContentPromptBuilder takes no dependency on
    /// Application.DTOs (it stays pure and testable without one), so this mapping — not a
    /// shared type — is what keeps the two layers decoupled. Not appended to or persisted
    /// anywhere: this request's turns exist only for the duration of this call.
    /// </summary>
    private static List<ContentGenerationTurn>? MapPreviousTurns(
        List<ContentGenerationTurnDto>? previousTurns)
    {
        if (previousTurns is null || previousTurns.Count == 0)
        {
            return null;
        }

        return previousTurns
            .Select(t => new ContentGenerationTurn(t.Instruction, t.Output))
            .ToList();
    }
}
