using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

/// <summary>
/// Stores and loads the posts on an Initiative's activity feed.
/// </summary>
public interface IActivityRepository
{
    /// <summary>Loads one post, or null when it does not exist.</summary>
    Task<Activity?> GetByIdAsync(int id);

    /// <summary>Returns every post on one Initiative's feed.</summary>
    Task<List<Activity>> GetByInitiativeIdAsync(int initiativeId);

    /// <summary>Inserts a new post and returns it with its generated id.</summary>
    Task<Activity> AddAsync(Activity activity);

    /// <summary>Saves changes to an existing post.</summary>
    Task UpdateAsync(Activity activity);

    /// <summary>
    /// Removes the post, first clearing SourceActivityId on any tasks it raised.
    /// </summary>
    /// <remarks>
    /// The foreign key is Restrict rather than SetNull — Initiatives already cascades
    /// into both Activity and Tasks, and a second path into Tasks is not allowed — so
    /// the references have to be detached here. The tasks themselves survive; work
    /// already assigned should not vanish because the post that raised it was removed.
    /// </remarks>
    Task RemoveAsync(Activity activity);

    /// <summary>Swaps the post's mentioned people for a new set.</summary>
    Task ReplaceMentionsAsync(int activityId, IReadOnlyCollection<int> mentionedUserIds);
}
