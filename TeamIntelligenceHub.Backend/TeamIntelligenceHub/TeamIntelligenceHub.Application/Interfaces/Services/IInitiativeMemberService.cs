using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// Manages who is on an Initiative's team, in what role, and at what allocation.
/// </summary>
public interface IInitiativeMemberService
{
    /// <summary>
    /// Returns the Initiative's team, each with their allocation summed across every
    /// Initiative.
    /// </summary>
    /// <param name="initiativeId">The Initiative whose team to load.</param>
    /// <returns>The team members.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the Initiative does not exist.
    /// </exception>
    Task<List<InitiativeMemberResponseDto>> GetByInitiativeAsync(int initiativeId);

    /// <summary>
    /// Adds an active user to the Initiative's team.
    /// </summary>
    /// <param name="initiativeId">The Initiative whose team to join.</param>
    /// <param name="request">The user, role, responsibility area, and allocation.</param>
    /// <returns>The new membership.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the Initiative does not exist.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown for an unknown, deactivated, or already-enrolled user or an invalid role or
    /// allocation.
    /// </exception>
    Task<InitiativeMemberResponseDto> AddAsync(
        int initiativeId,
        AddInitiativeMemberRequestDto request);

    /// <summary>
    /// Updates a membership's role, responsibility area, and allocation.
    /// </summary>
    /// <param name="initiativeId">The Initiative the membership belongs to.</param>
    /// <param name="memberId">The membership to update.</param>
    /// <param name="request">The new role, responsibility area, and allocation.</param>
    /// <returns>The updated membership.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the membership is not on this Initiative.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown for an invalid role or allocation.
    /// </exception>
    Task<InitiativeMemberResponseDto> UpdateAsync(
        int initiativeId,
        int memberId,
        UpdateInitiativeMemberRequestDto request);

    /// <summary>
    /// Removes a person from the Initiative's team.
    /// </summary>
    /// <param name="initiativeId">The Initiative the membership belongs to.</param>
    /// <param name="memberId">The membership to remove.</param>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the membership is not on this Initiative.
    /// </exception>
    Task RemoveAsync(int initiativeId, int memberId);
}
