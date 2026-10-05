using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// Reads and writes comments and replies on a task.
/// </summary>
public interface ITaskCommentService
{
    /// <summary>
    /// Returns every comment on the task. Throws NotFoundException when the task does not
    /// exist.
    /// </summary>
    Task<List<TaskCommentResponseDto>> GetByTaskAsync(int taskId);

    /// <summary>
    /// Posts a comment or one-level reply authored by the caller. Throws NotFoundException
    /// for an unknown task and ValidationException for blank text, an invalid reply
    /// target, or an unknown mentioned user.
    /// </summary>
    Task<TaskCommentResponseDto> CreateAsync(
        int taskId,
        CreateTaskCommentRequestDto request);

    /// <summary>
    /// Updates the caller's own comment and replaces its mentions. Throws
    /// NotFoundException when the comment is not on this task and ValidationException when
    /// the caller is not its author.
    /// </summary>
    Task<TaskCommentResponseDto> UpdateAsync(
        int taskId,
        int commentId,
        UpdateTaskCommentRequestDto request);

    /// <summary>
    /// Deletes the caller's own comment, its replies, and their stored files. Throws
    /// NotFoundException when the comment is not on this task and ValidationException when
    /// the caller is not its author.
    /// </summary>
    Task RemoveAsync(int taskId, int commentId);
}
