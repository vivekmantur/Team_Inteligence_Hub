// 1. Get a comment by ID
// 2. Get the comments for a task
// 3. Get the replies to a comment
// 4. Add a comment
// 5. Update a comment
// 6. Remove a comment with its replies
// 7. Replace a comment's mentions

using Microsoft.EntityFrameworkCore;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of ITaskCommentRepository, covering comments, their replies, and
/// mentions.
/// </summary>
public class TaskCommentRepository : ITaskCommentRepository
{
    private readonly TeamIntelligenceHubDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskCommentRepository"/> class.
    /// </summary>
    /// <param name="context">The database context used to read and save task comments and their mentions.</param>
    public TaskCommentRepository(TeamIntelligenceHubDbContext context)
    {
        _context = context;
    }

    /// <summary>Builds the comment query with the author, mentions, and attachments included.</summary>
    private IQueryable<TaskComment> WithDetail()
    {
        return _context.TaskComments
            .Include(x => x.User)
            .Include(x => x.Mentions)
                .ThenInclude(m => m.MentionedUser)
            .Include(x => x.Attachments);
    }

    /// <inheritdoc />
    public async Task<TaskComment?> GetByIdAsync(int id)
    {
        return await WithDetail().FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<List<TaskComment>> GetByTaskIdAsync(int taskId)
    {
        // Oldest first: a discussion reads top to bottom.
        return await WithDetail()
            .Where(x => x.TaskId == taskId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<TaskComment>> GetRepliesAsync(int parentCommentId)
    {
        return await _context.TaskComments
            .Where(x => x.ParentCommentId == parentCommentId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<TaskComment> AddAsync(TaskComment comment)
    {
        await _context.TaskComments.AddAsync(comment);

        await _context.SaveChangesAsync();

        return comment;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(TaskComment comment)
    {
        _context.TaskComments.Update(comment);

        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task RemoveWithRepliesAsync(TaskComment comment)
    {
        var replies = await _context.TaskComments
            .Where(x => x.ParentCommentId == comment.Id)
            .ToListAsync();

        if (replies.Count > 0)
        {
            // The self-referencing key is Restrict, so the database will not do this
            // for us. Mentions and attachments under each reply still cascade.
            _context.TaskComments.RemoveRange(replies);
        }

        _context.TaskComments.Remove(comment);

        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task ReplaceMentionsAsync(
        int commentId,
        IReadOnlyCollection<int> mentionedUserIds)
    {
        var existing = await _context.TaskCommentMentions
            .Where(x => x.TaskCommentId == commentId)
            .ToListAsync();

        _context.TaskCommentMentions.RemoveRange(existing);

        if (mentionedUserIds.Count > 0)
        {
            var now = DateTime.UtcNow;

            await _context.TaskCommentMentions.AddRangeAsync(
                mentionedUserIds.Select(userId => new TaskCommentMention
                {
                    TaskCommentId = commentId,
                    MentionedUserId = userId,
                    CreatedAt = now
                }));
        }

        await _context.SaveChangesAsync();
    }
}
