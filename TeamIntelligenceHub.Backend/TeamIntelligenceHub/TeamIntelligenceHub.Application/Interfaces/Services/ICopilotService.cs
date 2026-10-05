using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// Answers questions grounded on the indexed content.
/// </summary>
public interface ICopilotService
{
    /// <summary>
    /// Returns an answer with the chunks it was grounded on as citations, or suggested
    /// rephrasings when nothing relevant was found.
    /// </summary>
    /// <param name="question">The user's question.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The answer with its citations, or suggested questions when nothing matched.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown for a blank or overlong question.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.CopilotException">
    /// Thrown when the embedding, search, or chat call fails.
    /// </exception>
    Task<CopilotAnswerDto> AskAsync(
        string question,
        CancellationToken cancellationToken = default);
}
