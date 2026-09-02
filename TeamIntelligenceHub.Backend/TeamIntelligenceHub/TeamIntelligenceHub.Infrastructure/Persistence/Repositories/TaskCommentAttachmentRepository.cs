using Microsoft.EntityFrameworkCore;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Repositories;

public class TaskCommentAttachmentRepository : ITaskCommentAttachmentRepository
{
    private readonly TeamIntelligenceHubDbContext _context;

    public TaskCommentAttachmentRepository(TeamIntelligenceHubDbContext context)
    {
        _context = context;
    }

    public async Task<TaskCommentAttachment?> GetByIdAsync(int id)
    {
        return await _context.TaskCommentAttachments
            .Include(x => x.TaskComment)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<TaskCommentAttachment>> GetByCommentIdAsync(int commentId)
    {
        return await _context.TaskCommentAttachments
            .Where(x => x.TaskCommentId == commentId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

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

    public async Task<TaskCommentAttachment> AddAsync(TaskCommentAttachment attachment)
    {
        await _context.TaskCommentAttachments.AddAsync(attachment);

        await _context.SaveChangesAsync();

        return attachment;
    }

    public async Task RemoveAsync(TaskCommentAttachment attachment)
    {
        _context.TaskCommentAttachments.Remove(attachment);

        await _context.SaveChangesAsync();
    }
}
