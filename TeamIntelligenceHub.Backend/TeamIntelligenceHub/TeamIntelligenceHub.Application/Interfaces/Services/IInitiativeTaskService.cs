using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// Reads and writes the tasks on an Initiative.
/// </summary>
public interface IInitiativeTaskService
{
    /// <summary>
    /// Returns every task on the Initiative.
    /// </summary>
    /// <param name="initiativeId">The Initiative whose tasks to load.</param>
    /// <returns>The tasks on the Initiative.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the Initiative does not exist.
    /// </exception>
    Task<List<TaskResponseDto>> GetByInitiativeAsync(int initiativeId);

    /// <summary>
    /// Returns one task.
    /// </summary>
    /// <param name="initiativeId">The Initiative that owns the task.</param>
    /// <param name="taskId">The task identifier.</param>
    /// <returns>The task.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the task is not on this Initiative.
    /// </exception>
    Task<TaskResponseDto> GetByIdAsync(int initiativeId, int taskId);

    /// <summary>
    /// Creates a task, records the caller as its creator, and enrolls the assignee on the
    /// Initiative's team.
    /// </summary>
    /// <param name="initiativeId">The Initiative the task belongs to.</param>
    /// <param name="request">The task's title, assignee, due date, priority, and status.</param>
    /// <returns>The created task.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the Initiative does not exist.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown for invalid input, an unknown or deactivated assignee, or a caller with no
    /// profile.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the token carries no Entra object id.
    /// </exception>
    Task<TaskResponseDto> CreateAsync(
        int initiativeId,
        CreateTaskRequestDto request);

    /// <summary>
    /// Updates a task and enrolls the assignee on the Initiative's team.
    /// </summary>
    /// <param name="initiativeId">The Initiative that owns the task.</param>
    /// <param name="taskId">The task to update.</param>
    /// <param name="request">The task's new details.</param>
    /// <returns>The updated task.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the task is not on this Initiative.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown for invalid input or an unknown or deactivated assignee.
    /// </exception>
    Task<TaskResponseDto> UpdateAsync(
        int initiativeId,
        int taskId,
        UpdateTaskRequestDto request);

    /// <summary>
    /// Deletes a task.
    /// </summary>
    /// <param name="initiativeId">The Initiative that owns the task.</param>
    /// <param name="taskId">The task to delete.</param>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the task is not on this Initiative.
    /// </exception>
    Task RemoveAsync(int initiativeId, int taskId);

    /// <summary>
    /// Raises a task from an activity post that mentioned someone.
    /// </summary>
    /// <remarks>
    /// Routed through this service rather than the repository so auto-created tasks get
    /// the same treatment as hand-made ones — notably enrolling the assignee on the
    /// Initiative's team.
    /// </remarks>
    /// <param name="initiativeId">The Initiative the post is on.</param>
    /// <param name="activityId">The post that raised the task.</param>
    /// <param name="assignedToUserId">The mentioned user who receives the task.</param>
    /// <param name="title">The task title, taken from the post.</param>
    /// <returns>The created task.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown for a blank title, an unknown or deactivated assignee, or a caller with no
    /// profile.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the token carries no Entra object id.
    /// </exception>
    Task<TaskResponseDto> CreateFromActivityAsync(
        int initiativeId,
        int activityId,
        int assignedToUserId,
        string title);
}
