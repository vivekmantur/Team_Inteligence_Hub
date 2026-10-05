using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

/// <summary>
/// Stores and loads the memberships that make up each Initiative's team.
/// </summary>
public interface IInitiativeMemberRepository
{
    /// <summary>Loads one membership, or null when it does not exist.</summary>
    Task<InitiativeMember?> GetByIdAsync(int id);

    /// <summary>Returns every membership on one Initiative's team.</summary>
    Task<List<InitiativeMember>> GetByInitiativeIdAsync(int initiativeId);

    /// <summary>
    /// Each user's Allocation summed across every Initiative they belong to, not just
    /// one — a capacity signal, so a user's row in a batch not appearing in the result
    /// means they have no tracked allocation anywhere (treat as 0).
    /// </summary>
    Task<Dictionary<int, decimal>> GetTotalAllocationByUserIdsAsync(
        IReadOnlyCollection<int> userIds);

    /// <summary>True when the user is already on the Initiative's team.</summary>
    Task<bool> ExistsAsync(int initiativeId, int userId);

    /// <summary>Inserts a new membership and returns it with its generated id.</summary>
    Task<InitiativeMember> AddAsync(InitiativeMember member);

    /// <summary>Saves changes to an existing membership.</summary>
    Task UpdateAsync(InitiativeMember member);

    /// <summary>Deletes the membership.</summary>
    Task RemoveAsync(InitiativeMember member);
}
