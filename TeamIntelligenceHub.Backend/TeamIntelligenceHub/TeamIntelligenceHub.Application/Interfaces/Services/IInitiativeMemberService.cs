using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

public interface IInitiativeMemberService
{
    Task<List<InitiativeMemberResponseDto>> GetByInitiativeAsync(int initiativeId);

    Task<InitiativeMemberResponseDto> AddAsync(
        int initiativeId,
        AddInitiativeMemberRequestDto request);

    Task<InitiativeMemberResponseDto> UpdateAsync(
        int initiativeId,
        int memberId,
        UpdateInitiativeMemberRequestDto request);

    Task RemoveAsync(int initiativeId, int memberId);
}
