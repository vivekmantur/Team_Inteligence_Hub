namespace TeamIntelligenceHub.Application.DTOs;

/// <summary>A person mentioned in a comment.</summary>
public class TaskCommentMentionDto
{
    public int MentionedUserId { get; set; }

    public string DisplayName { get; set; } = null!;
}

/// <summary>Metadata for a file attached to a comment.</summary>
public class TaskCommentAttachmentDto
{
    public int Id { get; set; }

    public string FileName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long FileSize { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>One comment or reply on a task, with its mentions and attachments.</summary>
public class TaskCommentResponseDto
{
    public int Id { get; set; }

    public int TaskId { get; set; }

    public int UserId { get; set; }

    public string UserDisplayName { get; set; } = null!;

    public int? ParentCommentId { get; set; }

    public string CommentText { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public List<TaskCommentMentionDto> Mentions { get; set; } = [];

    public List<TaskCommentAttachmentDto> Attachments { get; set; } = [];
}
