namespace TeamIntelligenceHub.Domain.Entities;

/// <summary>
/// Records that an activity post mentioned someone, so they can be notified and the
/// mention can be found later. One row per person mentioned.
/// </summary>
public class ActivityMention
{
    public int Id { get; set; }

    public int ActivityId { get; set; }

    public int MentionedUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties

    public Activity Activity { get; set; } = null!;

    public User MentionedUser { get; set; } = null!;
}
