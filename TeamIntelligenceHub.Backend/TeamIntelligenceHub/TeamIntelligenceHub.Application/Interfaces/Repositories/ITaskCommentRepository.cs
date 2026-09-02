using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

public interface ITaskCommentRepository
{
    Task<TaskComment?> GetByIdAsync(int id);

    Task<List<TaskComment>> GetByTaskIdAsync(int taskId);

    Task<List<TaskComment>> GetRepliesAsync(int parentCommentId);

    Task<TaskComment> AddAsync(TaskComment comment);

    Task UpdateAsync(TaskComment comment);

    /// <summary>
    /// Removes the comment together with any replies. Replies are deleted first because
    /// the self-referencing key cannot cascade in SQL Server.
    /// </summary>
    Task RemoveWithRepliesAsync(TaskComment comment);

    Task ReplaceMentionsAsync(int commentId, IReadOnlyCollection<int> mentionedUserIds);
}
