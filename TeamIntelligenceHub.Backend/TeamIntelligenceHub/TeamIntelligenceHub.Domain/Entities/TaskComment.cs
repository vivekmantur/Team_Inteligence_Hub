namespace TeamIntelligenceHub.Domain.Entities;

/// <summary>
/// A message in a task's discussion. Replies point at the comment they answer.
/// </summary>
public class TaskComment
{
    // Single source of truth for field limits, shared with the EF configuration and
    // the request DTO so a value can never pass validation only to fail at SQL.
    public const int CommentTextMinLength = 1;
    public const int CommentTextMaxLength = 4000;

    public int Id { get; set; }

    public int TaskId { get; set; }

    public int UserId { get; set; }

    /// <summary>
    /// Null for a top-level comment. Set to the comment being replied to.
    /// </summary>
    public int? ParentCommentId { get; set; }

    public string CommentText { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    /// <summary>Null until the comment is edited.</summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties

    public InitiativeTask Task { get; set; } = null!;

    /// <summary>Who wrote it.</summary>
    public User User { get; set; } = null!;

    public TaskComment? ParentComment { get; set; }

    public ICollection<TaskComment> Replies { get; set; }
        = new List<TaskComment>();

    public ICollection<TaskCommentMention> Mentions { get; set; }
        = new List<TaskCommentMention>();

    public ICollection<TaskCommentAttachment> Attachments { get; set; }
        = new List<TaskCommentAttachment>();
}
