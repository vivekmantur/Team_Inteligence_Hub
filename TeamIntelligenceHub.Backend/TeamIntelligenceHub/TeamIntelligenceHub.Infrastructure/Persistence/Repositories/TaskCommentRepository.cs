using Microsoft.EntityFrameworkCore;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Repositories;

public class TaskCommentRepository : ITaskCommentRepository
{
    private readonly TeamIntelligenceHubDbContext _context;

    public TaskCommentRepository(TeamIntelligenceHubDbContext context)
    {
        _context = context;
    }

    private IQueryable<TaskComment> WithDetail()
    {
        return _context.TaskComments
            .Include(x => x.User)
            .Include(x => x.Mentions)
                .ThenInclude(m => m.MentionedUser)
            .Include(x => x.Attachments);
    }

    public async Task<TaskComment?> GetByIdAsync(int id)
    {
        return await WithDetail().FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<TaskComment>> GetByTaskIdAsync(int taskId)
    {
        // Oldest first: a discussion reads top to bottom.
        return await WithDetail()
            .Where(x => x.TaskId == taskId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<TaskComment>> GetRepliesAsync(int parentCommentId)
    {
        return await _context.TaskComments
            .Where(x => x.ParentCommentId == parentCommentId)
            .ToListAsync();
    }

    public async Task<TaskComment> AddAsync(TaskComment comment)
    {
        await _context.TaskComments.AddAsync(comment);

        await _context.SaveChangesAsync();

        return comment;
    }

    public async Task UpdateAsync(TaskComment comment)
    {
        _context.TaskComments.Update(comment);

        await _context.SaveChangesAsync();
    }

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
