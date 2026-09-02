namespace TeamIntelligenceHub.Domain.Entities;

/// <summary>
/// Records that a comment mentioned someone, so they can be notified and the mention
/// can be found later. One row per person mentioned.
/// </summary>
public class TaskCommentMention
{
    public int Id { get; set; }

    public int TaskCommentId { get; set; }

    public int MentionedUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties

    public TaskComment TaskComment { get; set; } = null!;

    public User MentionedUser { get; set; } = null!;
}
