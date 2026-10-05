namespace TeamIntelligenceHub.Domain.Enums;

/// <summary>
/// Where a task sits in its lifecycle. Display labels ("Not Started") live in the UI;
/// these names are the stored and transmitted identifiers. Named to avoid colliding with
/// System.Threading.Tasks.TaskStatus.
/// </summary>
public enum InitiativeTaskStatus
{
    NotStarted,
    InProgress,
    Blocked,
    Done
}
