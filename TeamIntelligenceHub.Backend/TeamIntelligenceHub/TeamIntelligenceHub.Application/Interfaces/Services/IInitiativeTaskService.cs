using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

public interface IInitiativeTaskService
{
    Task<List<TaskResponseDto>> GetByInitiativeAsync(int initiativeId);

    Task<TaskResponseDto> GetByIdAsync(int initiativeId, int taskId);

    Task<TaskResponseDto> CreateAsync(
        int initiativeId,
        CreateTaskRequestDto request);

    Task<TaskResponseDto> UpdateAsync(
        int initiativeId,
        int taskId,
        UpdateTaskRequestDto request);

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
