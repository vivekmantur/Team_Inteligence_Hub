using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

public interface ITaskCommentAttachmentRepository
{
    Task<TaskCommentAttachment?> GetByIdAsync(int id);

    Task<List<TaskCommentAttachment>> GetByCommentIdAsync(int commentId);

    /// <summary>
    /// Every blob under a comment and its replies, so the files can be removed when the
    /// comment is deleted. Cascade clears the rows but never touches storage.
    /// </summary>
    Task<List<string>> GetBlobNamesForCommentTreeAsync(int commentId);

    Task<TaskCommentAttachment> AddAsync(TaskCommentAttachment attachment);

    Task RemoveAsync(TaskCommentAttachment attachment);
}
