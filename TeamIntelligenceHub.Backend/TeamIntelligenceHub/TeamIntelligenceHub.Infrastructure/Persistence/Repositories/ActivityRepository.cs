using Microsoft.EntityFrameworkCore;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Repositories;

public class ActivityRepository : IActivityRepository
{
    private readonly TeamIntelligenceHubDbContext _context;

    public ActivityRepository(TeamIntelligenceHubDbContext context)
    {
        _context = context;
    }

    private IQueryable<Activity> WithDetail()
    {
        return _context.Activities
            .Include(x => x.User)
            .Include(x => x.Mentions)
                .ThenInclude(m => m.MentionedUser)
            .Include(x => x.CreatedTasks)
                .ThenInclude(t => t.AssignedToUser);
    }

    public async Task<Activity?> GetByIdAsync(int id)
    {
        return await WithDetail().FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<Activity>> GetByInitiativeIdAsync(int initiativeId)
    {
        // Newest first: a feed is read from the top.
        return await WithDetail()
            .Where(x => x.InitiativeId == initiativeId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<Activity> AddAsync(Activity activity)
    {
        await _context.Activities.AddAsync(activity);

        await _context.SaveChangesAsync();

        return activity;
    }

    public async Task UpdateAsync(Activity activity)
    {
        _context.Activities.Update(activity);

        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(Activity activity)
    {
        var raisedTasks = await _context.Tasks
            .Where(x => x.SourceActivityId == activity.Id)
            .ToListAsync();

        foreach (var task in raisedTasks)
        {
            // Detach rather than delete. The post is going; the work is not.
            task.SourceActivityId = null;
        }

        _context.Activities.Remove(activity);

        await _context.SaveChangesAsync();
    }

    public async Task ReplaceMentionsAsync(
        int activityId,
        IReadOnlyCollection<int> mentionedUserIds)
    {
        var existing = await _context.ActivityMentions
            .Where(x => x.ActivityId == activityId)
            .ToListAsync();

        _context.ActivityMentions.RemoveRange(existing);

        if (mentionedUserIds.Count > 0)
        {
            var now = DateTime.UtcNow;

            await _context.ActivityMentions.AddRangeAsync(
                mentionedUserIds.Select(userId => new ActivityMention
                {
                    ActivityId = activityId,
                    MentionedUserId = userId,
                    CreatedAt = now
                }));
        }

        await _context.SaveChangesAsync();
    }
}
