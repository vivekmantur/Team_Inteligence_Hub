namespace TeamIntelligenceHub.Domain.Enums;

/// <summary>
/// How serious a flagged risk is. Ordered low to high.
/// </summary>
/// <remarks>
/// Shares its members with ContributionPriority but means something different: priority is
/// how soon the contribution wants attention, severity is how much damage the risk does.
/// Kept apart the way InitiativePriority and InitiativeTaskPriority are.
/// </remarks>
public enum RiskSeverity
{
    Low,
    Medium,
    High,
    Critical
}
