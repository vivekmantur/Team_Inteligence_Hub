using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// Reads, writes, and aggregates Initiatives.
/// </summary>
public interface IInitiativeService
{
    /// <summary>Returns one Initiative, or null when it does not exist.</summary>
    Task<InitiativeResponseDto?> GetByIdAsync(int id);

    /// <summary>Returns every Initiative.</summary>
    Task<List<InitiativeResponseDto>> GetAllAsync();

    /// <summary>
    /// Creates an Initiative, owned by the caller unless another owner is named. Throws
    /// ValidationException for invalid input, an unknown or deactivated owner or sponsor,
    /// or a duplicate name.
    /// </summary>
    Task<InitiativeResponseDto> CreateAsync(CreateInitiativeRequestDto request);

    /// <summary>
    /// Updates an Initiative. Throws NotFoundException when it does not exist and
    /// ValidationException for invalid input, an unknown or deactivated owner or sponsor,
    /// or a duplicate name.
    /// </summary>
    Task<InitiativeResponseDto> UpdateAsync(int id, UpdateInitiativeRequestDto request);

    /// <summary>
    /// What deleting this Initiative would also remove, for a confirmation dialog. Throws
    /// NotFoundException when it does not exist.
    /// </summary>
    Task<InitiativeDeletionImpactDto> GetDeletionImpactAsync(int id);

    /// <summary>
    /// Deletes an Initiative, everything under it, and its contributions' stored files.
    /// Throws NotFoundException when it does not exist.
    /// </summary>
    Task RemoveAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Aggregate counts for the Insights page's Readiness and Audience &amp; Roles tabs.</summary>
    Task<ReadinessInsightsDto> GetReadinessInsightsAsync();
}
