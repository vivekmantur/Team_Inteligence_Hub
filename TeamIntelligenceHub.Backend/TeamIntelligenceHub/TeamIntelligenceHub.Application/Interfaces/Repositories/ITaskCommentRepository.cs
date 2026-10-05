using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

/// <summary>
/// Stores and loads comments and replies on tasks.
/// </summary>
public interface ITaskCommentRepository
{
    /// <summary>
    /// Loads one comment.
    /// </summary>
    /// <param name="id">The comment identifier.</param>
    /// <returns>The comment, or null when it does not exist.</returns>
    Task<TaskComment?> GetByIdAsync(int id);

    /// <summary>
    /// Returns every comment on one task.
    /// </summary>
    /// <param name="taskId">The task whose comments to load.</param>
    /// <returns>The comments on the task.</returns>
    Task<List<TaskComment>> GetByTaskIdAsync(int taskId);

    /// <summary>
    /// Returns the replies to one comment.
    /// </summary>
    /// <param name="parentCommentId">The comment whose replies to load.</param>
    /// <returns>The replies to the comment.</returns>
    Task<List<TaskComment>> GetRepliesAsync(int parentCommentId);

    /// <summary>
    /// Inserts a new comment.
    /// </summary>
    /// <param name="comment">The comment to insert.</param>
    /// <returns>The comment with its generated id.</returns>
    Task<TaskComment> AddAsync(TaskComment comment);

    /// <summary>
    /// Saves changes to an existing comment.
    /// </summary>
    /// <param name="comment">The comment with its changes applied.</param>
    Task UpdateAsync(TaskComment comment);

    /// <summary>
    /// Removes the comment together with any replies. Replies are deleted first because
    /// the self-referencing key cannot cascade in SQL Server.
    /// </summary>
    /// <param name="comment">The comment to remove.</param>
    Task RemoveWithRepliesAsync(TaskComment comment);

    /// <summary>
    /// Swaps the comment's mentioned people for a new set.
    /// </summary>
    /// <param name="commentId">The comment whose mentions to replace.</param>
    /// <param name="mentionedUserIds">The users the comment now mentions.</param>
    Task ReplaceMentionsAsync(int commentId, IReadOnlyCollection<int> mentionedUserIds);
}
