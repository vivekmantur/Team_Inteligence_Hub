using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

public interface IInitiativeRepository
{
    Task<Initiative?> GetByIdAsync(int id);

    Task<List<Initiative>> GetAllAsync();

    Task<bool> NameExistsAsync(string name);

    Task<Initiative> AddAsync(Initiative initiative);
}
