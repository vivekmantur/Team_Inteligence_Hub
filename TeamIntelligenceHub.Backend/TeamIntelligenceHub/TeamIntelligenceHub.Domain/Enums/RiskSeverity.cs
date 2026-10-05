namespace TeamIntelligenceHub.Domain.Enums;

/// <summary>
/// How serious a flagged risk is. Ordered low to high. Unlike ContributionPriority, it
/// measures how much damage the risk does, not how soon it needs attention.
/// </summary>
public enum RiskSeverity
{
    Low,
    Medium,
    High,
    Critical
}
