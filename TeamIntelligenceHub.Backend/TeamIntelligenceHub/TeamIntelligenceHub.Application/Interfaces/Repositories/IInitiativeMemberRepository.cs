using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

public interface IInitiativeMemberRepository
{
    Task<InitiativeMember?> GetByIdAsync(int id);

    Task<List<InitiativeMember>> GetByInitiativeIdAsync(int initiativeId);

    /// <summary>
    /// Each user's Allocation summed across every Initiative they belong to, not just
    /// one — a capacity signal, so a user's row in a batch not appearing in the result
    /// means they have no tracked allocation anywhere (treat as 0).
    /// </summary>
    Task<Dictionary<int, decimal>> GetTotalAllocationByUserIdsAsync(
        IReadOnlyCollection<int> userIds);

    Task<bool> ExistsAsync(int initiativeId, int userId);

    Task<InitiativeMember> AddAsync(InitiativeMember member);

    Task UpdateAsync(InitiativeMember member);

    Task RemoveAsync(InitiativeMember member);
}
