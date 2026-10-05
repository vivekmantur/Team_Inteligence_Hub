using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// Generates one piece of Content Studio output for an Initiative.
/// </summary>
/// <remarks>
/// Loads the Initiative's structured data, builds the format-specific prompt, and calls
/// the chat model through IChatCompletionClient, the same client Copilot chat uses.
/// </remarks>
public interface IContentGenerationService
{
    /// <summary>
    /// Returns generated content for the requested format. Throws ValidationException for
    /// a missing selection, NotFoundException for an unknown Initiative, and
    /// CopilotException when the model returns nothing.
    /// </summary>
    Task<ContentGenerationResponseDto> GenerateAsync(
        int initiativeId,
        ContentGenerationRequestDto request,
        CancellationToken cancellationToken = default);
}
