// 1. Get an activity post by ID
// 2. Get the activity feed for an initiative
// 3. Add an activity post
// 4. Update an activity post
// 5. Remove an activity post, detaching the tasks raised from it
// 6. Replace an activity post's mentions

using Microsoft.EntityFrameworkCore;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IActivityRepository, loading posts with their author, mentions,
/// and raised tasks.
/// </summary>
public class ActivityRepository : IActivityRepository
{
    private readonly TeamIntelligenceHubDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityRepository"/> class.
    /// </summary>
    /// <param name="context">The database context used to read and save activity posts and their mentions.</param>
    public ActivityRepository(TeamIntelligenceHubDbContext context)
    {
        _context = context;
    }

    /// <summary>Builds the activity query with the author, mentions, and raised tasks included.</summary>
    private IQueryable<Activity> WithDetail()
    {
        return _context.Activities
            .Include(x => x.User)
            .Include(x => x.Mentions)
                .ThenInclude(m => m.MentionedUser)
            .Include(x => x.CreatedTasks)
                .ThenInclude(t => t.AssignedToUser);
    }

    /// <inheritdoc />
    public async Task<Activity?> GetByIdAsync(int id)
    {
        return await WithDetail().FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<List<Activity>> GetByInitiativeIdAsync(int initiativeId)
    {
        // Newest first: a feed is read from the top.
        return await WithDetail()
            .Where(x => x.InitiativeId == initiativeId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Activity> AddAsync(Activity activity)
    {
        await _context.Activities.AddAsync(activity);

        await _context.SaveChangesAsync();

        return activity;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Activity activity)
    {
        _context.Activities.Update(activity);

        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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
