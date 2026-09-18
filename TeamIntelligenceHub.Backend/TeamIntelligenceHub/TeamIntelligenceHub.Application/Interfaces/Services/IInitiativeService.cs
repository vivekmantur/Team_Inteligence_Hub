using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

public interface IInitiativeService
{
    Task<InitiativeResponseDto?> GetByIdAsync(int id);

    Task<List<InitiativeResponseDto>> GetAllAsync();

    Task<InitiativeResponseDto> CreateAsync(CreateInitiativeRequestDto request);

    Task<InitiativeResponseDto> UpdateAsync(int id, UpdateInitiativeRequestDto request);

    /// <summary>What deleting this Initiative would also remove, for a confirmation dialog.</summary>
    Task<InitiativeDeletionImpactDto> GetDeletionImpactAsync(int id);

    Task RemoveAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Aggregate counts for the Insights page's Readiness and Audience & Roles tabs.</summary>
    Task<ReadinessInsightsDto> GetReadinessInsightsAsync();
}
