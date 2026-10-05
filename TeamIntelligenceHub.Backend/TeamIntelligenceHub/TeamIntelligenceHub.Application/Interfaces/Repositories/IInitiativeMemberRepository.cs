using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

/// <summary>
/// Stores and loads the memberships that make up each Initiative's team.
/// </summary>
public interface IInitiativeMemberRepository
{
    /// <summary>
    /// Loads one membership.
    /// </summary>
    /// <param name="id">The membership identifier.</param>
    /// <returns>The membership, or null when it does not exist.</returns>
    Task<InitiativeMember?> GetByIdAsync(int id);

    /// <summary>
    /// Returns every membership on one Initiative's team.
    /// </summary>
    /// <param name="initiativeId">The Initiative whose team to load.</param>
    /// <returns>The memberships on the team.</returns>
    Task<List<InitiativeMember>> GetByInitiativeIdAsync(int initiativeId);

    /// <summary>
    /// Each user's Allocation summed across every Initiative they belong to, not just
    /// one — a capacity signal, so a user's row in a batch not appearing in the result
    /// means they have no tracked allocation anywhere (treat as 0).
    /// </summary>
    /// <param name="userIds">The users whose allocation to total.</param>
    /// <returns>Each user's total allocation, keyed by user id.</returns>
    Task<Dictionary<int, decimal>> GetTotalAllocationByUserIdsAsync(
        IReadOnlyCollection<int> userIds);

    /// <summary>
    /// Checks whether the user is already on the Initiative's team.
    /// </summary>
    /// <param name="initiativeId">The Initiative to check.</param>
    /// <param name="userId">The user to look for.</param>
    /// <returns>True when the user is already on the team.</returns>
    Task<bool> ExistsAsync(int initiativeId, int userId);

    /// <summary>
    /// Inserts a new membership.
    /// </summary>
    /// <param name="member">The membership to insert.</param>
    /// <returns>The membership with its generated id.</returns>
    Task<InitiativeMember> AddAsync(InitiativeMember member);

    /// <summary>
    /// Saves changes to an existing membership.
    /// </summary>
    /// <param name="member">The membership with its changes applied.</param>
    Task UpdateAsync(InitiativeMember member);

    /// <summary>
    /// Deletes the membership.
    /// </summary>
    /// <param name="member">The membership to delete.</param>
    Task RemoveAsync(InitiativeMember member);
}
