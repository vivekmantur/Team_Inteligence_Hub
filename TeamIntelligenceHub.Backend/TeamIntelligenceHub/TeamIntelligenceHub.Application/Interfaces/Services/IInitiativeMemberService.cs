using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// Manages who is on an Initiative's team, in what role, and at what allocation.
/// </summary>
public interface IInitiativeMemberService
{
    /// <summary>
    /// Returns the Initiative's team, each with their allocation summed across every
    /// Initiative. Throws NotFoundException when the Initiative does not exist.
    /// </summary>
    Task<List<InitiativeMemberResponseDto>> GetByInitiativeAsync(int initiativeId);

    /// <summary>
    /// Adds an active user to the Initiative's team. Throws NotFoundException for an
    /// unknown Initiative and ValidationException for an unknown, deactivated, or
    /// already-enrolled user or an invalid role or allocation.
    /// </summary>
    Task<InitiativeMemberResponseDto> AddAsync(
        int initiativeId,
        AddInitiativeMemberRequestDto request);

    /// <summary>
    /// Updates a membership's role, responsibility area, and allocation. Throws
    /// NotFoundException when the membership is not on this Initiative and
    /// ValidationException for an invalid role or allocation.
    /// </summary>
    Task<InitiativeMemberResponseDto> UpdateAsync(
        int initiativeId,
        int memberId,
        UpdateInitiativeMemberRequestDto request);

    /// <summary>
    /// Removes a person from the Initiative's team. Throws NotFoundException when the
    /// membership is not on this Initiative.
    /// </summary>
    Task RemoveAsync(int initiativeId, int memberId);
}
