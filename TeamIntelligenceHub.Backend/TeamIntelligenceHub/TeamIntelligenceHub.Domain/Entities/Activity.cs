namespace TeamIntelligenceHub.Domain.Entities;

/// <summary>
/// A post on an Initiative's activity feed.
/// </summary>
/// <remarks>
/// When AutoCreateTaskEnabled is set, posting spawns a task for each person mentioned.
/// Those tasks point back through Tasks.SourceActivityId rather than the feed pointing
/// at them, because one post can raise several tasks.
/// </remarks>
public class Activity
{
    // Single source of truth for field limits, shared with the EF configuration and
    // the request DTO so a value can never pass validation only to fail at SQL.
    public const int ActivityMessageMinLength = 1;
    public const int ActivityMessageMaxLength = 4000;

    public int Id { get; set; }

    public int InitiativeId { get; set; }

    /// <summary>Who posted it.</summary>
    public int UserId { get; set; }

    public string ActivityMessage { get; set; } = null!;

    /// <summary>Whether mentions on this post should raise tasks.</summary>
    public bool AutoCreateTaskEnabled { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Null until the post is edited.</summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties

    public Initiative Initiative { get; set; } = null!;

    public User User { get; set; } = null!;

    public ICollection<ActivityMention> Mentions { get; set; }
        = new List<ActivityMention>();

    /// <summary>Tasks this post raised. Empty unless AutoCreateTaskEnabled was set.</summary>
    public ICollection<InitiativeTask> CreatedTasks { get; set; }
        = new List<InitiativeTask>();
}
