using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// Generates one piece of Content Studio output for an Initiative.
/// </summary>
/// <remarks>
/// Loads the Initiative's structured data, builds the format-specific prompt, and calls
/// Azure OpenAI through IChatCompletionClient — the same provider Copilot chat uses.
/// </remarks>
public interface IContentGenerationService
{
    Task<ContentGenerationResponseDto> GenerateAsync(
        int initiativeId,
        ContentGenerationRequestDto request,
        CancellationToken cancellationToken = default);
}
