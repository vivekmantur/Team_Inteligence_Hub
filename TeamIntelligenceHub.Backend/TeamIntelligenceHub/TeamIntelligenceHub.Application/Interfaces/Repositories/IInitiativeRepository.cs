using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

public interface IInitiativeRepository
{
    Task<Initiative?> GetByIdAsync(int id);

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

    Task<Initiative> AddAsync(Initiative initiative);

    Task UpdateAsync(Initiative initiative);

    Task RemoveAsync(Initiative initiative);

    /// <summary>
    /// Counts of the rows a delete would also remove — Contributions, Tasks, Activity,
    /// and Team members — for the deletion-confirmation dialog.
    /// </summary>
    Task<(int Contributions, int Tasks, int Activities, int Members)> GetDeletionImpactAsync(
        int initiativeId);
}
