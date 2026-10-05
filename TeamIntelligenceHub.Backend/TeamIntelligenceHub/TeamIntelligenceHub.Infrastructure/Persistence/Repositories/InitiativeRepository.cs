// 1. Get an initiative by ID
// 2. Get all initiatives
// 3. Get the initiatives used for readiness insights
// 4. Check whether an initiative name is already taken
// 5. Add an initiative
// 6. Update an initiative
// 7. Remove an initiative
// 8. Count the records that deleting an initiative removes

using Microsoft.EntityFrameworkCore;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IInitiativeRepository, loading Initiatives with their owner and
/// executive sponsor.
/// </summary>
public class InitiativeRepository : IInitiativeRepository
{
    private readonly TeamIntelligenceHubDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="InitiativeRepository"/> class.
    /// </summary>
    /// <param name="context">The database context used to read and save initiatives.</param>
    public InitiativeRepository(TeamIntelligenceHubDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Initiative?> GetByIdAsync(int id)
    {
        return await _context.Initiatives
            .Include(x => x.Owner)
            .Include(x => x.ExecutiveSponsor)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<List<Initiative>> GetAllAsync()
    {
        return await _context.Initiatives
            .Include(x => x.Owner)
            .Include(x => x.ExecutiveSponsor)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Initiative>> GetForReadinessInsightsAsync()
    {
        return await _context.Initiatives.ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> NameExistsAsync(string name, int? excludeInitiativeId = null)
    {
        return await _context.Initiatives
            .AnyAsync(x => x.Name == name && x.Id != excludeInitiativeId);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task UpdateAsync(Initiative initiative)
    {
        _context.Initiatives.Update(initiative);

        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task RemoveAsync(Initiative initiative)
    {
        // Contributions, InitiativeTasks, Activities, and InitiativeMembers all cascade
        // in the database; the caller is responsible for anything cascade delete does
        // not reach, such as blob attachments.
        _context.Initiatives.Remove(initiative);

        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
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
