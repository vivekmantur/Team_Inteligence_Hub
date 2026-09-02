using Microsoft.EntityFrameworkCore;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Repositories;

public class InitiativeTaskRepository : IInitiativeTaskRepository
{
    private readonly TeamIntelligenceHubDbContext _context;

    public InitiativeTaskRepository(TeamIntelligenceHubDbContext context)
    {
        _context = context;
    }

    public async Task<InitiativeTask?> GetByIdAsync(int id)
    {
        return await _context.Tasks
            .Include(x => x.AssignedToUser)
            .Include(x => x.CreatedByUser)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<InitiativeTask>> GetByInitiativeIdAsync(int initiativeId)
    {
        return await _context.Tasks
            .Include(x => x.AssignedToUser)
            .Include(x => x.CreatedByUser)
            .Where(x => x.InitiativeId == initiativeId)
            // Dated work first, oldest deadline leading; undated falls to the back.
            .OrderBy(x => x.DueDate == null)
            .ThenBy(x => x.DueDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<InitiativeTask> AddAsync(InitiativeTask task)
    {
        await _context.Tasks.AddAsync(task);

        await _context.SaveChangesAsync();

        // Reload so the response can carry assignee and creator names.
        await _context.Entry(task)
            .Reference(x => x.CreatedByUser)
            .LoadAsync();

        if (task.AssignedToUserId.HasValue)
        {
            await _context.Entry(task)
                .Reference(x => x.AssignedToUser)
                .LoadAsync();
        }

        return task;
    }

    public async Task UpdateAsync(InitiativeTask task)
    {
        _context.Tasks.Update(task);

        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(InitiativeTask task)
    {
        _context.Tasks.Remove(task);

        await _context.SaveChangesAsync();
    }
}
