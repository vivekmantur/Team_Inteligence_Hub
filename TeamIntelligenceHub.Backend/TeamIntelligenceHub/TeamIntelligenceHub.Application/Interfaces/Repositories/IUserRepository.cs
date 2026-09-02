using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);

    Task<User?> GetByEntraObjectIdAsync(string entraObjectId);

    Task<List<User>> GetAllAsync();

    Task<User> AddAsync(User user);

    Task UpdateAsync(User user);
}