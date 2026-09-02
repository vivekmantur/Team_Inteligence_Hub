using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

public interface IInitiativeTaskRepository
{
    Task<InitiativeTask?> GetByIdAsync(int id);

    Task<List<InitiativeTask>> GetByInitiativeIdAsync(int initiativeId);

    Task<InitiativeTask> AddAsync(InitiativeTask task);

    Task UpdateAsync(InitiativeTask task);

    Task RemoveAsync(InitiativeTask task);
}
