// 1. Get a comment attachment by ID
// 2. Get the attachments for a comment
// 3. Get the blob names for a comment and its replies
// 4. Add a comment attachment
// 5. Remove a comment attachment

using Microsoft.EntityFrameworkCore;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of ITaskCommentAttachmentRepository for comment attachment metadata
/// rows.
/// </summary>
public class TaskCommentAttachmentRepository : ITaskCommentAttachmentRepository
{
    private readonly TeamIntelligenceHubDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskCommentAttachmentRepository"/> class.
    /// </summary>
    /// <param name="context">The database context used to read and save task comment attachment rows.</param>
    public TaskCommentAttachmentRepository(TeamIntelligenceHubDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<TaskCommentAttachment?> GetByIdAsync(int id)
    {
        return await _context.TaskCommentAttachments
            .Include(x => x.TaskComment)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<List<TaskCommentAttachment>> GetByCommentIdAsync(int commentId)
    {
        return await _context.TaskCommentAttachments
            .Where(x => x.TaskCommentId == commentId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<string>> GetBlobNamesForCommentTreeAsync(int commentId)
    {
        var replyIds = await _context.TaskComments
            .Where(x => x.ParentCommentId == commentId)
            .Select(x => x.Id)
            .ToListAsync();

        replyIds.Add(commentId);

        return await _context.TaskCommentAttachments
            .Where(x => replyIds.Contains(x.TaskCommentId))
            .Select(x => x.BlobName)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<TaskCommentAttachment> AddAsync(TaskCommentAttachment attachment)
    {
        await _context.TaskCommentAttachments.AddAsync(attachment);

        await _context.SaveChangesAsync();

        return attachment;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(TaskCommentAttachment attachment)
    {
        _context.TaskCommentAttachments.Remove(attachment);

        await _context.SaveChangesAsync();
    }
}
