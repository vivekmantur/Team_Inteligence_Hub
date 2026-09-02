using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

public interface IActivityRepository
{
    Task<Activity?> GetByIdAsync(int id);

    Task<List<Activity>> GetByInitiativeIdAsync(int initiativeId);

    Task<Activity> AddAsync(Activity activity);

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

    Task ReplaceMentionsAsync(int activityId, IReadOnlyCollection<int> mentionedUserIds);
}
