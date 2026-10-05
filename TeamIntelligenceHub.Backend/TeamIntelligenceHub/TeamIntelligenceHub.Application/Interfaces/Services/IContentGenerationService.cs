using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// Generates one piece of Content Studio output for an Initiative from its structured
/// data, through the same chat client Copilot uses.
/// </summary>
public interface IContentGenerationService
{
    /// <summary>
    /// Returns generated content for the requested format.
    /// </summary>
    /// <param name="initiativeId">The Initiative the content is about.</param>
    /// <param name="request">The format, tone, audience, length, instructions, and earlier turns.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The generated content and the format it was written for.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown when the format, tone, audience, or length is missing.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the Initiative does not exist.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.CopilotException">
    /// Thrown when the chat call fails or the model returns nothing.
    /// </exception>
    Task<ContentGenerationResponseDto> GenerateAsync(
        int initiativeId,
        ContentGenerationRequestDto request,
        CancellationToken cancellationToken = default);
}
