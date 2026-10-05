using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

/// <summary>
/// Stores and loads the local user rows that mirror Entra identities.
/// </summary>
public interface IUserRepository
{
    /// <summary>Loads one user by local id, or null when it does not exist.</summary>
    Task<User?> GetByIdAsync(int id);

    /// <summary>Loads one user by Entra object id, or null when no row matches.</summary>
    Task<User?> GetByEntraObjectIdAsync(string entraObjectId);

    /// <summary>Returns every user.</summary>
    Task<List<User>> GetAllAsync();

    /// <summary>Inserts a new user and returns it with its generated id.</summary>
    Task<User> AddAsync(User user);

    /// <summary>Saves changes to an existing user.</summary>
    Task UpdateAsync(User user);
}
