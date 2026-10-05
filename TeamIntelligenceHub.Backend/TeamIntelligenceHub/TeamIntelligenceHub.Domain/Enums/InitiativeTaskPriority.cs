namespace TeamIntelligenceHub.Domain.Enums;

/// <summary>
/// How urgent a task is. Ordered high to low. Kept separate from
/// <see cref="InitiativePriority"/> so the two can diverge.
/// </summary>
public enum InitiativeTaskPriority
{
    High,
    Medium,
    Low
}
