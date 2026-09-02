using System.ComponentModel.DataAnnotations;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.DTOs;

/// <summary>
/// Payload for posting to an Initiative's activity feed.
/// </summary>
/// <remarks>
/// The Initiative comes from the route and the author from the bearer token. Mentions
/// arrive as user ids because the client already resolved each "@name" in its picker.
/// </remarks>
public class CreateActivityRequestDto
{
    [Required(ErrorMessage = "An update is required.")]
    [StringLength(
        Activity.ActivityMessageMaxLength,
        MinimumLength = Activity.ActivityMessageMinLength,
        ErrorMessage = "An update must be between {2} and {1} characters.")]
    public string ActivityMessage { get; set; } = null!;

    /// <summary>Raise a task for each person mentioned, the author excepted.</summary>
    public bool AutoCreateTaskEnabled { get; set; }

    public List<int>? MentionedUserIds { get; set; }
}

/// <summary>
/// Payload for editing a post. Auto-create does not run again — tasks are raised once,
/// when the update is first posted.
/// </summary>
public class UpdateActivityRequestDto
{
    [Required(ErrorMessage = "An update is required.")]
    [StringLength(
        Activity.ActivityMessageMaxLength,
        MinimumLength = Activity.ActivityMessageMinLength,
        ErrorMessage = "An update must be between {2} and {1} characters.")]
    public string ActivityMessage { get; set; } = null!;

    /// <summary>Replaces the mention list outright.</summary>
    public List<int>? MentionedUserIds { get; set; }
}

public class ActivityMentionDto
{
    public int MentionedUserId { get; set; }

    public string DisplayName { get; set; } = null!;
}

/// <summary>A task this post raised, enough for the feed to link to it.</summary>
public class ActivityCreatedTaskDto
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public int? AssignedToUserId { get; set; }

    public string? AssignedToDisplayName { get; set; }
}

public class ActivityResponseDto
{
    public int Id { get; set; }

    public int InitiativeId { get; set; }

    public int UserId { get; set; }

    public string UserDisplayName { get; set; } = null!;

    public string ActivityMessage { get; set; } = null!;

    public bool AutoCreateTaskEnabled { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public List<ActivityMentionDto> Mentions { get; set; } = [];

    public List<ActivityCreatedTaskDto> CreatedTasks { get; set; } = [];
}
