namespace TeamIntelligenceHub.Domain.Enums;

/// <summary>
/// Where a task sits in its lifecycle. Display labels ("Not Started") live in the UI —
/// these names are the stored and transmitted identifiers.
/// </summary>
/// <remarks>
/// Named InitiativeTaskStatus rather than TaskStatus, which would collide with
/// System.Threading.Tasks.TaskStatus.
/// </remarks>
public enum InitiativeTaskStatus
{
    NotStarted,
    InProgress,
    Blocked,
    Done
}
