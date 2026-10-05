using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// Reads and writes comments and replies on a task.
/// </summary>
public interface ITaskCommentService
{
    /// <summary>
    /// Returns every comment on the task.
    /// </summary>
    /// <param name="taskId">The task whose comments to load.</param>
    /// <returns>The comments on the task.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the task does not exist.
    /// </exception>
    Task<List<TaskCommentResponseDto>> GetByTaskAsync(int taskId);

    /// <summary>
    /// Posts a comment or one-level reply authored by the caller.
    /// </summary>
    /// <param name="taskId">The task to comment on.</param>
    /// <param name="request">The comment text, optional parent comment, and mentions.</param>
    /// <returns>The created comment.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the task does not exist.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown for blank text, an invalid reply target, an unknown mentioned user, or a
    /// caller with no profile.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the token carries no Entra object id.
    /// </exception>
    Task<TaskCommentResponseDto> CreateAsync(
        int taskId,
        CreateTaskCommentRequestDto request);

    /// <summary>
    /// Updates the caller's own comment and replaces its mentions.
    /// </summary>
    /// <param name="taskId">The task the comment is on.</param>
    /// <param name="commentId">The comment to update.</param>
    /// <param name="request">The new comment text and mentions.</param>
    /// <returns>The updated comment.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the comment is not on this task.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown when the caller is not the author, or for blank text or an unknown
    /// mentioned user.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the token carries no Entra object id.
    /// </exception>
    Task<TaskCommentResponseDto> UpdateAsync(
        int taskId,
        int commentId,
        UpdateTaskCommentRequestDto request);

    /// <summary>
    /// Deletes the caller's own comment, its replies, and their stored files.
    /// </summary>
    /// <param name="taskId">The task the comment is on.</param>
    /// <param name="commentId">The comment to delete.</param>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the comment is not on this task.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown when the caller is not the author.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the token carries no Entra object id.
    /// </exception>
    Task RemoveAsync(int taskId, int commentId);
}
