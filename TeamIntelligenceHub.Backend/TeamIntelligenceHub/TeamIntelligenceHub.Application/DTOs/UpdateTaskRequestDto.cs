using System.ComponentModel.DataAnnotations;
using TeamIntelligenceHub.Domain.Entities;
using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Application.DTOs;

/// <summary>
/// Payload for editing a task. Every field is replaced, so omitting one clears it —
/// send the current value to keep it.
/// </summary>
public class UpdateTaskRequestDto
{
    [Required(ErrorMessage = "Task title is required.")]
    [StringLength(
        InitiativeTask.TitleMaxLength,
        MinimumLength = InitiativeTask.TitleMinLength,
        ErrorMessage = "Task title must be between {2} and {1} characters.")]
    public string Title { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "Assignee is not a valid user.")]
    public int? AssignedToUserId { get; set; }

    public DateOnly? DueDate { get; set; }

    public InitiativeTaskPriority? Priority { get; set; }

    public InitiativeTaskStatus? Status { get; set; }
}
