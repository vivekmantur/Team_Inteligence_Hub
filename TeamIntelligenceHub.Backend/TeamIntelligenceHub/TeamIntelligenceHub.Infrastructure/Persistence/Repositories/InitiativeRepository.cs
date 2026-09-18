using Microsoft.EntityFrameworkCore;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Repositories;

public class InitiativeRepository : IInitiativeRepository
{
    private readonly TeamIntelligenceHubDbContext _context;

    public InitiativeRepository(TeamIntelligenceHubDbContext context)
    {
        _context = context;
    }

    public async Task<Initiative?> GetByIdAsync(int id)
    {
        return await _context.Initiatives
            .Include(x => x.Owner)
            .Include(x => x.ExecutiveSponsor)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<Initiative>> GetAllAsync()
    {
        return await _context.Initiatives
            .Include(x => x.Owner)
            .Include(x => x.ExecutiveSponsor)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Initiative>> GetForReadinessInsightsAsync()
    {
        return await _context.Initiatives.ToListAsync();
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeInitiativeId = null)
    {
        return await _context.Initiatives
            .AnyAsync(x => x.Name == name && x.Id != excludeInitiativeId);
    }

    public async Task<Initiative> AddAsync(Initiative initiative)
    {
        await _context.Initiatives.AddAsync(initiative);

        await _context.SaveChangesAsync();

        // Reload so the response carries owner and sponsor names.
        await _context.Entry(initiative)
            .Reference(x => x.Owner)
            .LoadAsync();

        if (initiative.ExecutiveSponsorUserId.HasValue)
        {
            await _context.Entry(initiative)
                .Reference(x => x.ExecutiveSponsor)
                .LoadAsync();
        }

        return initiative;
    }

    public async Task UpdateAsync(Initiative initiative)
    {
        _context.Initiatives.Update(initiative);

        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(Initiative initiative)
    {
        // Contributions, InitiativeTasks, Activities, and InitiativeMembers all cascade
        // in the database — the caller is responsible for anything cascade delete does
        // not reach, such as blob attachments.
        _context.Initiatives.Remove(initiative);

        await _context.SaveChangesAsync();
    }

    public async Task<(int Contributions, int Tasks, int Activities, int Members)>
        GetDeletionImpactAsync(int initiativeId)
    {
        var contributions = await _context.Contributions
            .CountAsync(x => x.InitiativeId == initiativeId);
        var tasks = await _context.Tasks
            .CountAsync(x => x.InitiativeId == initiativeId);
        var activities = await _context.Activities
            .CountAsync(x => x.InitiativeId == initiativeId);
        var members = await _context.InitiativeMembers
            .CountAsync(x => x.InitiativeId == initiativeId);

        return (contributions, tasks, activities, members);
    }
}
