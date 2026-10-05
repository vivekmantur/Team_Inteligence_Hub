// 1. Generate Content Studio output for an Initiative

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
/// the chat model, through IChatCompletionClient, for one piece of Content Studio output.
/// Every format uses structured data only; this service never retrieves attachments.
/// </summary>
public class ContentGenerationService : IContentGenerationService
{
    private readonly IInitiativeRepository _initiativeRepository;
    private readonly IContributionRepository _contributionRepository;
    private readonly IChatCompletionClient _chatClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentGenerationService"/> class.
    /// </summary>
    /// <param name="initiativeRepository">The repository that loads the Initiative.</param>
    /// <param name="contributionRepository">The repository that loads the Initiative's contributions.</param>
    /// <param name="chatClient">The chat model client that writes the content.</param>
    public ContentGenerationService(
        IInitiativeRepository initiativeRepository,
        IContributionRepository contributionRepository,
        IChatCompletionClient chatClient)
    {
        _initiativeRepository = initiativeRepository;
        _contributionRepository = contributionRepository;
        _chatClient = chatClient;
    }

    /// <inheritdoc />
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
    /// [Required] on the request DTO rejects a missing value for a caller coming through
    /// the controller; this re-check keeps the service safe to call directly (e.g. from
    /// tests), where no model validation runs.
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

    /// <summary>Trims and turns whitespace-only into null, so a blank instruction adds nothing to the prompt.</summary>
    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();

        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary>
    /// Maps the request's PreviousTurns DTOs to ContentGenerationTurn, so the prompt
    /// builder stays free of any dependency on Application.DTOs. The turns are not
    /// persisted; they live only for this call.
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
