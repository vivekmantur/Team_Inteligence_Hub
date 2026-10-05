using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

/// <summary>
/// Stores and loads the metadata rows for files attached to task comments.
/// </summary>
public interface ITaskCommentAttachmentRepository
{
    /// <summary>
    /// Loads one attachment row.
    /// </summary>
    /// <param name="id">The attachment identifier.</param>
    /// <returns>The attachment row, or null when it does not exist.</returns>
    Task<TaskCommentAttachment?> GetByIdAsync(int id);

    /// <summary>
    /// Returns every attachment row on one comment.
    /// </summary>
    /// <param name="commentId">The comment whose attachments to load.</param>
    /// <returns>The attachment rows on the comment.</returns>
    Task<List<TaskCommentAttachment>> GetByCommentIdAsync(int commentId);

    /// <summary>
    /// Every blob under a comment and its replies, so the files can be removed when the
    /// comment is deleted. Cascade clears the rows but never touches storage.
    /// </summary>
    /// <param name="commentId">The root comment of the tree.</param>
    /// <returns>The blob names of every attachment in the comment tree.</returns>
    Task<List<string>> GetBlobNamesForCommentTreeAsync(int commentId);

    /// <summary>
    /// Inserts a new attachment row.
    /// </summary>
    /// <param name="attachment">The attachment row to insert.</param>
    /// <returns>The attachment row with its generated id.</returns>
    Task<TaskCommentAttachment> AddAsync(TaskCommentAttachment attachment);

    /// <summary>
    /// Deletes the attachment row. The stored file is not touched.
    /// </summary>
    /// <param name="attachment">The attachment row to delete.</param>
    Task RemoveAsync(TaskCommentAttachment attachment);
}
