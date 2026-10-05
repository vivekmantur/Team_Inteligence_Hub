using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

/// <summary>
/// Stores and loads comments and replies on tasks.
/// </summary>
public interface ITaskCommentRepository
{
    /// <summary>Loads one comment, or null when it does not exist.</summary>
    Task<TaskComment?> GetByIdAsync(int id);

    /// <summary>Returns every comment on one task.</summary>
    Task<List<TaskComment>> GetByTaskIdAsync(int taskId);

    /// <summary>Returns the replies to one comment.</summary>
    Task<List<TaskComment>> GetRepliesAsync(int parentCommentId);

    /// <summary>Inserts a new comment and returns it with its generated id.</summary>
    Task<TaskComment> AddAsync(TaskComment comment);

    /// <summary>Saves changes to an existing comment.</summary>
    Task UpdateAsync(TaskComment comment);

    /// <summary>
    /// Removes the comment together with any replies. Replies are deleted first because
    /// the self-referencing key cannot cascade in SQL Server.
    /// </summary>
    Task RemoveWithRepliesAsync(TaskComment comment);

    /// <summary>Swaps the comment's mentioned people for a new set.</summary>
    Task ReplaceMentionsAsync(int commentId, IReadOnlyCollection<int> mentionedUserIds);
}
