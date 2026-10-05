using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

/// <summary>
/// Stores and loads the tasks on each Initiative.
/// </summary>
public interface IInitiativeTaskRepository
{
    /// <summary>
    /// Loads one task.
    /// </summary>
    /// <param name="id">The task identifier.</param>
    /// <returns>The task, or null when it does not exist.</returns>
    Task<InitiativeTask?> GetByIdAsync(int id);

    /// <summary>
    /// Returns every task on one Initiative.
    /// </summary>
    /// <param name="initiativeId">The Initiative whose tasks to load.</param>
    /// <returns>The tasks on the Initiative.</returns>
    Task<List<InitiativeTask>> GetByInitiativeIdAsync(int initiativeId);

    /// <summary>
    /// Inserts a new task.
    /// </summary>
    /// <param name="task">The task to insert.</param>
    /// <returns>The task with its generated id.</returns>
    Task<InitiativeTask> AddAsync(InitiativeTask task);

    /// <summary>
    /// Saves changes to an existing task.
    /// </summary>
    /// <param name="task">The task with its changes applied.</param>
    Task UpdateAsync(InitiativeTask task);

    /// <summary>
    /// Deletes the task.
    /// </summary>
    /// <param name="task">The task to delete.</param>
    Task RemoveAsync(InitiativeTask task);
}
