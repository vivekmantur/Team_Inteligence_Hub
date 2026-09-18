using Microsoft.EntityFrameworkCore;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Repositories;

public class InitiativeMemberRepository : IInitiativeMemberRepository
{
    private readonly TeamIntelligenceHubDbContext _context;

    public InitiativeMemberRepository(TeamIntelligenceHubDbContext context)
    {
        _context = context;
    }

    public async Task<InitiativeMember?> GetByIdAsync(int id)
    {
        return await _context.InitiativeMembers
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<InitiativeMember>> GetByInitiativeIdAsync(int initiativeId)
    {
        return await _context.InitiativeMembers
            .Include(x => x.User)
            .Where(x => x.InitiativeId == initiativeId)
            .OrderBy(x => x.JoinedAt)
            .ToListAsync();
    }

    public async Task<Dictionary<int, decimal>> GetTotalAllocationByUserIdsAsync(
        IReadOnlyCollection<int> userIds)
    {
        return await _context.InitiativeMembers
            .Where(x => userIds.Contains(x.UserId))
            .GroupBy(x => x.UserId)
            // Null means "not tracked" for that one row, not "0% on that Initiative" —
            // but a sum has to treat it as something, and 0 is the only value that
            // doesn't overstate a person's real workload.
            .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.Allocation ?? 0) })
            .ToDictionaryAsync(x => x.UserId, x => x.Total);
    }

    public async Task<bool> ExistsAsync(int initiativeId, int userId)
    {
        return await _context.InitiativeMembers
            .AnyAsync(x => x.InitiativeId == initiativeId && x.UserId == userId);
    }

    public async Task<InitiativeMember> AddAsync(InitiativeMember member)
    {
        await _context.InitiativeMembers.AddAsync(member);

        await _context.SaveChangesAsync();

        // Reload so the response can carry the person's name and address.
        await _context.Entry(member)
            .Reference(x => x.User)
            .LoadAsync();

        return member;
    }

    public async Task UpdateAsync(InitiativeMember member)
    {
        _context.InitiativeMembers.Update(member);

        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(InitiativeMember member)
    {
        _context.InitiativeMembers.Remove(member);

        await _context.SaveChangesAsync();
    }
}
