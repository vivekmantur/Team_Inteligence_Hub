using System.ComponentModel.DataAnnotations;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.DTOs;

/// <summary>
/// Payload for posting a comment or a reply.
/// </summary>
/// <remarks>
/// The task comes from the route and the author from the bearer token, so neither can be
/// set by the body. Mentions are sent as user ids, not parsed out of the text — the client
/// already knows who it resolved each "@name" to.
/// </remarks>
public class CreateTaskCommentRequestDto
{
    [Required(ErrorMessage = "Comment text is required.")]
    [StringLength(
        TaskComment.CommentTextMaxLength,
        MinimumLength = TaskComment.CommentTextMinLength,
        ErrorMessage = "A comment must be between {2} and {1} characters.")]
    public string CommentText { get; set; } = null!;

    /// <summary>Null posts to the thread root; set it to reply to a comment.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Parent comment is not valid.")]
    public int? ParentCommentId { get; set; }

    /// <summary>People mentioned. Duplicates are collapsed.</summary>
    public List<int>? MentionedUserIds { get; set; }
}
