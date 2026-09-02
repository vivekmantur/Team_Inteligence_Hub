using System.ComponentModel.DataAnnotations;
using TeamIntelligenceHub.Domain.Entities;
using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Application.DTOs;

/// <summary>
/// Payload for creating a task.
/// </summary>
/// <remarks>
/// The Initiative comes from the route and the creator from the bearer token, so neither
/// can be spoofed by the body.
/// </remarks>
public class CreateTaskRequestDto
{
    [Required(ErrorMessage = "Task title is required.")]
    [StringLength(
        InitiativeTask.TitleMaxLength,
        MinimumLength = InitiativeTask.TitleMinLength,
        ErrorMessage = "Task title must be between {2} and {1} characters.")]
    public string Title { get; set; } = null!;

    /// <summary>Null leaves the task unassigned.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Assignee is not a valid user.")]
    public int? AssignedToUserId { get; set; }

    public DateOnly? DueDate { get; set; }

    /// <summary>Defaults to Medium when omitted. An unknown value is a 400.</summary>
    public InitiativeTaskPriority? Priority { get; set; }

    /// <summary>Defaults to NotStarted when omitted.</summary>
    public InitiativeTaskStatus? Status { get; set; }
}
