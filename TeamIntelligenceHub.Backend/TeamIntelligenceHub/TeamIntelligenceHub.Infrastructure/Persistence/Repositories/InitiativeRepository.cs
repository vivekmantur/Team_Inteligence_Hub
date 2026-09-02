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

    public async Task<bool> NameExistsAsync(string name)
    {
        return await _context.Initiatives
            .AnyAsync(x => x.Name == name);
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
}
