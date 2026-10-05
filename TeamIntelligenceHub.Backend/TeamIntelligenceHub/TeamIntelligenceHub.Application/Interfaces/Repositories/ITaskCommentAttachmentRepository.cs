using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

/// <summary>
/// Stores and loads the metadata rows for files attached to task comments.
/// </summary>
public interface ITaskCommentAttachmentRepository
{
    /// <summary>Loads one attachment row, or null when it does not exist.</summary>
    Task<TaskCommentAttachment?> GetByIdAsync(int id);

    /// <summary>Returns every attachment row on one comment.</summary>
    Task<List<TaskCommentAttachment>> GetByCommentIdAsync(int commentId);

    /// <summary>
    /// Every blob under a comment and its replies, so the files can be removed when the
    /// comment is deleted. Cascade clears the rows but never touches storage.
    /// </summary>
    Task<List<string>> GetBlobNamesForCommentTreeAsync(int commentId);

    /// <summary>Inserts a new attachment row and returns it with its generated id.</summary>
    Task<TaskCommentAttachment> AddAsync(TaskCommentAttachment attachment);

    /// <summary>Deletes the attachment row. The stored file is not touched.</summary>
    Task RemoveAsync(TaskCommentAttachment attachment);
}
