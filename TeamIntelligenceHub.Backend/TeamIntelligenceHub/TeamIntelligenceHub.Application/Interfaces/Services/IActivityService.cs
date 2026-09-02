using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

public interface IActivityService
{
    Task<List<ActivityResponseDto>> GetByInitiativeAsync(int initiativeId);

    Task<ActivityResponseDto> CreateAsync(
        int initiativeId,
        CreateActivityRequestDto request);

    Task<ActivityResponseDto> UpdateAsync(
        int initiativeId,
        int activityId,
        UpdateActivityRequestDto request);

    Task RemoveAsync(int initiativeId, int activityId);
}
