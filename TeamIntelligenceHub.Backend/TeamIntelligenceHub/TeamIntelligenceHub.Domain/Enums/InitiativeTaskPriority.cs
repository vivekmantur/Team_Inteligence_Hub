namespace TeamIntelligenceHub.Domain.Enums;

/// <summary>
/// How urgent a task is. Ordered high to low.
/// </summary>
/// <remarks>
/// Deliberately separate from <see cref="InitiativePriority"/>. They share values today,
/// but they are different concepts and should be free to diverge.
/// </remarks>
public enum InitiativeTaskPriority
{
    High,
    Medium,
    Low
}
