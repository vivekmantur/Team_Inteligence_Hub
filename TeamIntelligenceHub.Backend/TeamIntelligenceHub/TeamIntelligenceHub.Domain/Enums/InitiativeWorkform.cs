namespace TeamIntelligenceHub.Domain.Enums;

/// <summary>
/// What form the Initiative's work takes. A closed set rather than free text, so the
/// value can be filtered and reported on consistently.
/// </summary>
public enum InitiativeWorkform
{
    Motion,
    Rollout,
    Pilot,
    Campaign,
    ResearchOrAssessment,
    ContentDevelopment,
    OperationalImprovement,
    ReportingOrAnalytics,
    CommunityOrEngagementMotion
}
