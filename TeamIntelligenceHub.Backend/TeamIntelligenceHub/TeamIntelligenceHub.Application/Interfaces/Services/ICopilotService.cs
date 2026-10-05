using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// Answers questions grounded on the indexed content.
/// </summary>
public interface ICopilotService
{
    /// <summary>
    /// Returns an answer with the chunks it was grounded on as citations, or suggested
    /// rephrasings when nothing relevant was found. Throws ValidationException for a blank
    /// or overlong question.
    /// </summary>
    Task<CopilotAnswerDto> AskAsync(
        string question,
        CancellationToken cancellationToken = default);
}
