using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// Reads, writes, and aggregates Initiatives.
/// </summary>
public interface IInitiativeService
{
    /// <summary>
    /// Returns one Initiative.
    /// </summary>
    /// <param name="id">The Initiative identifier.</param>
    /// <returns>The Initiative, or null when it does not exist.</returns>
    Task<InitiativeResponseDto?> GetByIdAsync(int id);

    /// <summary>
    /// Returns every Initiative.
    /// </summary>
    /// <returns>All Initiatives.</returns>
    Task<List<InitiativeResponseDto>> GetAllAsync();

    /// <summary>
    /// Creates an Initiative, owned by the caller unless another owner is named.
    /// </summary>
    /// <param name="request">The Initiative's details.</param>
    /// <returns>The created Initiative.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown for invalid input, an unknown or deactivated owner or sponsor, a duplicate
    /// name, or a caller with no profile.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the token carries no Entra object id.
    /// </exception>
    Task<InitiativeResponseDto> CreateAsync(CreateInitiativeRequestDto request);

    /// <summary>
    /// Updates an Initiative.
    /// </summary>
    /// <param name="id">The Initiative to update.</param>
    /// <param name="request">The Initiative's new details.</param>
    /// <returns>The updated Initiative.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the Initiative does not exist.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown for invalid input, an unknown or deactivated owner or sponsor, or a
    /// duplicate name.
    /// </exception>
    Task<InitiativeResponseDto> UpdateAsync(int id, UpdateInitiativeRequestDto request);

    /// <summary>
    /// Returns what deleting this Initiative would also remove, for a confirmation dialog.
    /// </summary>
    /// <param name="id">The Initiative to check.</param>
    /// <returns>The counts of contributions, tasks, activities, and team members a delete removes.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the Initiative does not exist.
    /// </exception>
    Task<InitiativeDeletionImpactDto> GetDeletionImpactAsync(int id);

    /// <summary>
    /// Deletes an Initiative, everything under it, and its contributions' stored files.
    /// </summary>
    /// <param name="id">The Initiative to delete.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the Initiative does not exist.
    /// </exception>
    Task RemoveAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns aggregate counts for the Insights page's Readiness and Audience &amp; Roles tabs.
    /// </summary>
    /// <returns>The readiness counts and per-role coverage.</returns>
    Task<ReadinessInsightsDto> GetReadinessInsightsAsync();
}
