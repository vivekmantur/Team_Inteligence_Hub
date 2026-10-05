using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// Reads and writes posts on an Initiative's activity feed.
/// </summary>
public interface IActivityService
{
    /// <summary>
    /// Returns every post on the Initiative's feed.
    /// </summary>
    /// <param name="initiativeId">The Initiative whose feed to load.</param>
    /// <returns>The posts on the feed.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the Initiative does not exist.
    /// </exception>
    Task<List<ActivityResponseDto>> GetByInitiativeAsync(int initiativeId);

    /// <summary>
    /// Creates a post authored by the caller and, when requested, raises a task for each
    /// person mentioned.
    /// </summary>
    /// <param name="initiativeId">The Initiative whose feed receives the post.</param>
    /// <param name="request">The message, mentions, and auto-create setting.</param>
    /// <returns>The created post with its mentions and raised tasks.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the Initiative does not exist.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown for a blank message, an unknown mentioned user, or a caller with no profile.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the token carries no Entra object id.
    /// </exception>
    Task<ActivityResponseDto> CreateAsync(
        int initiativeId,
        CreateActivityRequestDto request);

    /// <summary>
    /// Updates the caller's own post and replaces its mentions.
    /// </summary>
    /// <param name="initiativeId">The Initiative the post belongs to.</param>
    /// <param name="activityId">The post to update.</param>
    /// <param name="request">The new message and mentions.</param>
    /// <returns>The updated post.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the post is not on this Initiative.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown when the caller is not the author, or for a blank message or an unknown
    /// mentioned user.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the token carries no Entra object id.
    /// </exception>
    Task<ActivityResponseDto> UpdateAsync(
        int initiativeId,
        int activityId,
        UpdateActivityRequestDto request);

    /// <summary>
    /// Deletes the caller's own post; tasks it raised survive.
    /// </summary>
    /// <param name="initiativeId">The Initiative the post belongs to.</param>
    /// <param name="activityId">The post to delete.</param>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the post is not on this Initiative.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown when the caller is not the author.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the token carries no Entra object id.
    /// </exception>
    Task RemoveAsync(int initiativeId, int activityId);
}
