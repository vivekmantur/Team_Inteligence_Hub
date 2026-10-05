namespace TeamIntelligenceHub.Domain.Enums;

/// <summary>
/// How urgent a contribution is. Ordered low to high. Kept separate from
/// InitiativePriority, which has no Critical member.
/// </summary>
public enum ContributionPriority
{
    Low,
    Medium,
    High,
    Critical
}
