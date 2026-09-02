using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

public interface ITaskCommentService
{
    Task<List<TaskCommentResponseDto>> GetByTaskAsync(int taskId);

    Task<TaskCommentResponseDto> CreateAsync(
        int taskId,
        CreateTaskCommentRequestDto request);

    Task<TaskCommentResponseDto> UpdateAsync(
        int taskId,
        int commentId,
        UpdateTaskCommentRequestDto request);

    Task RemoveAsync(int taskId, int commentId);
}
