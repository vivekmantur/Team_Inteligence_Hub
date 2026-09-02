namespace TeamIntelligenceHub.Domain.Enums;

/// <summary>
/// How urgent a contribution is. Ordered low to high.
/// </summary>
/// <remarks>
/// Deliberately separate from InitiativePriority, which has no Critical member.
/// </remarks>
public enum ContributionPriority
{
    Low,
    Medium,
    High,
    Critical
}
