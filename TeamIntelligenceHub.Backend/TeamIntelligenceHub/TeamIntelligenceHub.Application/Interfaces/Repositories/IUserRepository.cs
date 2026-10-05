using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

/// <summary>
/// Stores and loads the local user rows that mirror Entra identities.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Loads one user by local id.
    /// </summary>
    /// <param name="id">The local user identifier.</param>
    /// <returns>The user, or null when it does not exist.</returns>
    Task<User?> GetByIdAsync(int id);

    /// <summary>
    /// Loads one user by Entra object id.
    /// </summary>
    /// <param name="entraObjectId">The user's Entra object id.</param>
    /// <returns>The user, or null when no row matches.</returns>
    Task<User?> GetByEntraObjectIdAsync(string entraObjectId);

    /// <summary>
    /// Returns every user.
    /// </summary>
    /// <returns>All users.</returns>
    Task<List<User>> GetAllAsync();

    /// <summary>
    /// Inserts a new user.
    /// </summary>
    /// <param name="user">The user to insert.</param>
    /// <returns>The user with its generated id.</returns>
    Task<User> AddAsync(User user);

    /// <summary>
    /// Saves changes to an existing user.
    /// </summary>
    /// <param name="user">The user with its changes applied.</param>
    Task UpdateAsync(User user);
}
