// 1. Get a task by ID
// 2. Get the tasks for an initiative
// 3. Add a task
// 4. Update a task
// 5. Remove a task

using Microsoft.EntityFrameworkCore;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IInitiativeTaskRepository, loading tasks with their assignee and
/// creator.
/// </summary>
public class InitiativeTaskRepository : IInitiativeTaskRepository
{
    private readonly TeamIntelligenceHubDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="InitiativeTaskRepository"/> class.
    /// </summary>
    /// <param name="context">The database context used to read and save tasks.</param>
    public InitiativeTaskRepository(TeamIntelligenceHubDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<InitiativeTask?> GetByIdAsync(int id)
    {
        return await _context.Tasks
            .Include(x => x.AssignedToUser)
            .Include(x => x.CreatedByUser)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task UpdateAsync(InitiativeTask task)
    {
        _context.Tasks.Update(task);

        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task RemoveAsync(InitiativeTask task)
    {
        _context.Tasks.Remove(task);

        await _context.SaveChangesAsync();
    }
}
