namespace TeamIntelligenceHub.Domain.Enums;

/// <summary>
/// Initial indication of how disruptive an Initiative's change is expected to be for
/// impacted roles. A separate, coarser scale from Contribution's RiskSeverity —
/// deliberately not shared, since the two measure different things.
/// </summary>
public enum InitiativeChangeImpact
{
    Low,
    Medium,
    High
}
