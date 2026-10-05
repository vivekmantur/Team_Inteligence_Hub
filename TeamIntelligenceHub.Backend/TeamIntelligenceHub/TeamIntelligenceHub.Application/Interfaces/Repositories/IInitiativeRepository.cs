using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

/// <summary>
/// Stores and loads Initiatives.
/// </summary>
public interface IInitiativeRepository
{
    /// <summary>Loads one Initiative with its Owner and Executive Sponsor, or null when it does not exist.</summary>
    Task<Initiative?> GetByIdAsync(int id);

    /// <summary>Returns every Initiative with its Owner and Executive Sponsor.</summary>
    Task<List<Initiative>> GetAllAsync();

    /// <summary>
    /// Every Initiative's Status, Health, ImpactedRoles, and ChangeImpact — the only
    /// columns the Insights aggregates need. No Owner/ExecutiveSponsor Include, unlike
    /// GetAllAsync, since nothing here reads either.
    /// </summary>
    Task<List<Initiative>> GetForReadinessInsightsAsync();

    /// <summary>
    /// True when another Initiative already has this name. Pass excludeInitiativeId when
    /// editing, so a row is never compared against itself.
    /// </summary>
    Task<bool> NameExistsAsync(string name, int? excludeInitiativeId = null);

    /// <summary>Inserts a new Initiative and returns it with its generated id.</summary>
    Task<Initiative> AddAsync(Initiative initiative);

    /// <summary>Saves changes to an existing Initiative.</summary>
    Task UpdateAsync(Initiative initiative);

    /// <summary>
    /// Deletes the Initiative. Its database children cascade; stored files do not, so the
    /// caller removes those first.
    /// </summary>
    Task RemoveAsync(Initiative initiative);

    /// <summary>
    /// Counts of the rows a delete would also remove — Contributions, Tasks, Activity,
    /// and Team members — for the deletion-confirmation dialog.
    /// </summary>
    Task<(int Contributions, int Tasks, int Activities, int Members)> GetDeletionImpactAsync(
        int initiativeId);
}
