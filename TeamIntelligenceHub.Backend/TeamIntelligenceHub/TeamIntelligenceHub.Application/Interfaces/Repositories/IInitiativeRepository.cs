using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

/// <summary>
/// Stores and loads Initiatives.
/// </summary>
public interface IInitiativeRepository
{
    /// <summary>
    /// Loads one Initiative with its Owner and Executive Sponsor.
    /// </summary>
    /// <param name="id">The Initiative identifier.</param>
    /// <returns>The Initiative, or null when it does not exist.</returns>
    Task<Initiative?> GetByIdAsync(int id);

    /// <summary>
    /// Returns every Initiative with its Owner and Executive Sponsor.
    /// </summary>
    /// <returns>All Initiatives.</returns>
    Task<List<Initiative>> GetAllAsync();

    /// <summary>
    /// Every Initiative's Status, Health, ImpactedRoles, and ChangeImpact — the only
    /// columns the Insights aggregates need. No Owner/ExecutiveSponsor Include, unlike
    /// GetAllAsync, since nothing here reads either.
    /// </summary>
    /// <returns>All Initiatives with only the readiness columns loaded.</returns>
    Task<List<Initiative>> GetForReadinessInsightsAsync();

    /// <summary>
    /// True when another Initiative already has this name. Pass excludeInitiativeId when
    /// editing, so a row is never compared against itself.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <param name="excludeInitiativeId">The Initiative to leave out of the check, if any.</param>
    /// <returns>True when another Initiative already uses the name.</returns>
    Task<bool> NameExistsAsync(string name, int? excludeInitiativeId = null);

    /// <summary>
    /// Inserts a new Initiative.
    /// </summary>
    /// <param name="initiative">The Initiative to insert.</param>
    /// <returns>The Initiative with its generated id.</returns>
    Task<Initiative> AddAsync(Initiative initiative);

    /// <summary>
    /// Saves changes to an existing Initiative.
    /// </summary>
    /// <param name="initiative">The Initiative with its changes applied.</param>
    Task UpdateAsync(Initiative initiative);

    /// <summary>
    /// Deletes the Initiative. Its database children cascade; stored files do not, so the
    /// caller removes those first.
    /// </summary>
    /// <param name="initiative">The Initiative to delete.</param>
    Task RemoveAsync(Initiative initiative);

    /// <summary>
    /// Counts of the rows a delete would also remove — Contributions, Tasks, Activity,
    /// and Team members — for the deletion-confirmation dialog.
    /// </summary>
    /// <param name="initiativeId">The Initiative whose delete impact to count.</param>
    /// <returns>The number of Contributions, Tasks, Activities, and Members a delete removes.</returns>
    Task<(int Contributions, int Tasks, int Activities, int Members)> GetDeletionImpactAsync(
        int initiativeId);
}
