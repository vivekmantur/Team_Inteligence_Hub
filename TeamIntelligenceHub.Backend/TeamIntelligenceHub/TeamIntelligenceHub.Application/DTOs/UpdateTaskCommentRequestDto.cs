using System.ComponentModel.DataAnnotations;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.DTOs;

/// <summary>
/// Payload for editing a comment. The thread position cannot change — a reply stays a
/// reply to the same comment.
/// </summary>
public class UpdateTaskCommentRequestDto
{
    [Required(ErrorMessage = "Comment text is required.")]
    [StringLength(
        TaskComment.CommentTextMaxLength,
        MinimumLength = TaskComment.CommentTextMinLength,
        ErrorMessage = "A comment must be between {2} and {1} characters.")]
    public string CommentText { get; set; } = null!;

    /// <summary>Replaces the mention list outright.</summary>
    public List<int>? MentionedUserIds { get; set; }
}
