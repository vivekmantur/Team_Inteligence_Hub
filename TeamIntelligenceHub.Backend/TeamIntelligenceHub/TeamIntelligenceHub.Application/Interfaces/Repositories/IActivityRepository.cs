using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

/// <summary>
/// Stores and loads the posts on an Initiative's activity feed.
/// </summary>
public interface IActivityRepository
{
    /// <summary>
    /// Loads one post.
    /// </summary>
    /// <param name="id">The post identifier.</param>
    /// <returns>The post, or null when it does not exist.</returns>
    Task<Activity?> GetByIdAsync(int id);

    /// <summary>
    /// Returns every post on one Initiative's feed.
    /// </summary>
    /// <param name="initiativeId">The Initiative whose feed to load.</param>
    /// <returns>The posts on the feed.</returns>
    Task<List<Activity>> GetByInitiativeIdAsync(int initiativeId);

    /// <summary>
    /// Inserts a new post.
    /// </summary>
    /// <param name="activity">The post to insert.</param>
    /// <returns>The post with its generated id.</returns>
    Task<Activity> AddAsync(Activity activity);

    /// <summary>
    /// Saves changes to an existing post.
    /// </summary>
    /// <param name="activity">The post with its changes applied.</param>
    Task UpdateAsync(Activity activity);

    /// <summary>
    /// Removes the post, first clearing SourceActivityId on any tasks it raised.
    /// </summary>
    /// <param name="activity">The post to remove.</param>
    /// <remarks>
    /// The foreign key is Restrict rather than SetNull — Initiatives already cascades
    /// into both Activity and Tasks, and a second path into Tasks is not allowed — so
    /// the references have to be detached here. The tasks themselves survive; work
    /// already assigned should not vanish because the post that raised it was removed.
    /// </remarks>
    Task RemoveAsync(Activity activity);

    /// <summary>
    /// Swaps the post's mentioned people for a new set.
    /// </summary>
    /// <param name="activityId">The post whose mentions to replace.</param>
    /// <param name="mentionedUserIds">The users the post now mentions.</param>
    Task ReplaceMentionsAsync(int activityId, IReadOnlyCollection<int> mentionedUserIds);
}
