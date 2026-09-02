using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

public interface ICopilotService
{
    Task<CopilotAnswerDto> AskAsync(
        string question,
        CancellationToken cancellationToken = default);
}
