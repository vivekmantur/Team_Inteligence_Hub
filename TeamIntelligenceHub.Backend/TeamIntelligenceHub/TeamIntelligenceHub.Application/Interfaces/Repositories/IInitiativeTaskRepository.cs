using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

/// <summary>
/// Stores and loads the tasks on each Initiative.
/// </summary>
public interface IInitiativeTaskRepository
{
    /// <summary>Loads one task, or null when it does not exist.</summary>
    Task<InitiativeTask?> GetByIdAsync(int id);

    /// <summary>Returns every task on one Initiative.</summary>
    Task<List<InitiativeTask>> GetByInitiativeIdAsync(int initiativeId);

    /// <summary>Inserts a new task and returns it with its generated id.</summary>
    Task<InitiativeTask> AddAsync(InitiativeTask task);

    /// <summary>Saves changes to an existing task.</summary>
    Task UpdateAsync(InitiativeTask task);

    /// <summary>Deletes the task.</summary>
    Task RemoveAsync(InitiativeTask task);
}
