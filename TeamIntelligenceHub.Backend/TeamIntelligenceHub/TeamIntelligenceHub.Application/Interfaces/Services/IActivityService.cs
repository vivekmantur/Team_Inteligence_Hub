using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// Reads and writes posts on an Initiative's activity feed.
/// </summary>
public interface IActivityService
{
    /// <summary>
    /// Returns every post on the Initiative's feed. Throws NotFoundException when the
    /// Initiative does not exist.
    /// </summary>
    Task<List<ActivityResponseDto>> GetByInitiativeAsync(int initiativeId);

    /// <summary>
    /// Creates a post authored by the caller and, when requested, raises a task for each
    /// person mentioned. Throws NotFoundException for an unknown Initiative and
    /// ValidationException for a blank message or an unknown mentioned user.
    /// </summary>
    Task<ActivityResponseDto> CreateAsync(
        int initiativeId,
        CreateActivityRequestDto request);

    /// <summary>
    /// Updates the caller's own post and replaces its mentions. Throws NotFoundException
    /// when the post is not on this Initiative and ValidationException when the caller is
    /// not its author.
    /// </summary>
    Task<ActivityResponseDto> UpdateAsync(
        int initiativeId,
        int activityId,
        UpdateActivityRequestDto request);

    /// <summary>
    /// Deletes the caller's own post; tasks it raised survive. Throws NotFoundException
    /// when the post is not on this Initiative and ValidationException when the caller is
    /// not its author.
    /// </summary>
    Task RemoveAsync(int initiativeId, int activityId);
}
