using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Application.DTOs;

/// <summary>One task on an Initiative, with assignee and creator display names.</summary>
public class TaskResponseDto
{
    public int Id { get; set; }

    public int InitiativeId { get; set; }

    public string Title { get; set; } = null!;

    public int? AssignedToUserId { get; set; }

    public string? AssignedToDisplayName { get; set; }

    public int CreatedByUserId { get; set; }

    public string? CreatedByDisplayName { get; set; }

    public DateOnly? DueDate { get; set; }

    public InitiativeTaskPriority Priority { get; set; }

    public InitiativeTaskStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
