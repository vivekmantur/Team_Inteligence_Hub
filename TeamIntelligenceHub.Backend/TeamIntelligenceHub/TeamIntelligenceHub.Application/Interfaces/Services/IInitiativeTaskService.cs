using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// Reads and writes the tasks on an Initiative.
/// </summary>
public interface IInitiativeTaskService
{
    /// <summary>
    /// Returns every task on the Initiative. Throws NotFoundException when the Initiative
    /// does not exist.
    /// </summary>
    Task<List<TaskResponseDto>> GetByInitiativeAsync(int initiativeId);

    /// <summary>
    /// Returns one task. Throws NotFoundException when the task is not on this Initiative.
    /// </summary>
    Task<TaskResponseDto> GetByIdAsync(int initiativeId, int taskId);

    /// <summary>
    /// Creates a task, records the caller as its creator, and enrolls the assignee on the
    /// Initiative's team. Throws NotFoundException for an unknown Initiative and
    /// ValidationException for invalid input or an unknown or deactivated assignee.
    /// </summary>
    Task<TaskResponseDto> CreateAsync(
        int initiativeId,
        CreateTaskRequestDto request);

    /// <summary>
    /// Updates a task and enrolls the assignee on the Initiative's team. Throws
    /// NotFoundException when the task is not on this Initiative and ValidationException
    /// for invalid input or an unknown or deactivated assignee.
    /// </summary>
    Task<TaskResponseDto> UpdateAsync(
        int initiativeId,
        int taskId,
        UpdateTaskRequestDto request);

    /// <summary>
    /// Deletes a task. Throws NotFoundException when the task is not on this Initiative.
    /// </summary>
    Task RemoveAsync(int initiativeId, int taskId);

    /// <summary>
    /// Raises a task from an activity post that mentioned someone.
    /// </summary>
    /// <remarks>
    /// Routed through this service rather than the repository so auto-created tasks get
    /// the same treatment as hand-made ones — notably enrolling the assignee on the
    /// Initiative's team.
    /// </remarks>
    Task<TaskResponseDto> CreateFromActivityAsync(
        int initiativeId,
        int activityId,
        int assignedToUserId,
        string title);
}
