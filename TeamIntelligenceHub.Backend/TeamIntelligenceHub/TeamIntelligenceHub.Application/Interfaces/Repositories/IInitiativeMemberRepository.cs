using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

public interface IInitiativeMemberRepository
{
    Task<InitiativeMember?> GetByIdAsync(int id);

    Task<List<InitiativeMember>> GetByInitiativeIdAsync(int initiativeId);

    Task<bool> ExistsAsync(int initiativeId, int userId);

    Task<InitiativeMember> AddAsync(InitiativeMember member);

    Task UpdateAsync(InitiativeMember member);

    Task RemoveAsync(InitiativeMember member);
}
