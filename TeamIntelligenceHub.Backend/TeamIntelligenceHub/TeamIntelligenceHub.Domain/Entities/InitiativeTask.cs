using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Domain.Entities;

/// <summary>
/// A unit of work on an Initiative.
/// </summary>
/// <remarks>
/// Maps to the "Tasks" table. The C# type is named InitiativeTask because "Task" would
/// collide with System.Threading.Tasks.Task throughout an async codebase.
/// </remarks>
public class InitiativeTask
{
    // Single source of truth for field limits, shared with the EF configuration and
    // the request DTO so a value can never pass validation only to fail at SQL.
    public const int TitleMinLength = 3;
    public const int TitleMaxLength = 255;
    public const int EnumValueMaxLength = 50;

    /// <summary>
    /// Floor for Due Date. Also catches an omitted date, which model binding would
    /// otherwise turn into DateOnly's default of 0001-01-01.
    /// </summary>
    public static readonly DateOnly EarliestDueDate = new(2000, 1, 1);

    public int Id { get; set; }

    public int InitiativeId { get; set; }

    public string Title { get; set; } = null!;

    /// <summary>Null while the task is unassigned.</summary>
    public int? AssignedToUserId { get; set; }

    public int CreatedByUserId { get; set; }

    public DateOnly? DueDate { get; set; }

    /// <summary>
    /// The activity post that raised this task, when it was auto-created from a mention.
    /// Null for tasks created directly.
    /// </summary>
    public int? SourceActivityId { get; set; }

    public InitiativeTaskPriority Priority { get; set; }

    public InitiativeTaskStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Null until the task is first edited.</summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties

    public Initiative Initiative { get; set; } = null!;

    public User? AssignedToUser { get; set; }

    public User CreatedByUser { get; set; } = null!;

    public Activity? SourceActivity { get; set; }

    /// <summary>The discussion thread on this task.</summary>
    public ICollection<TaskComment> Comments { get; set; }
        = new List<TaskComment>();
}
