namespace TeamIntelligenceHub.Domain.Enums;

/// <summary>
/// A downstream surface that may draw on a contribution. It records the author's consent
/// for reuse, not who can see the contribution.
/// </summary>
public enum ContributionReuseTarget
{
    ExecutiveBrief,
    LeadershipUpdate,
    Qbr,
    CustomerStoryLibrary,
    KnowledgeRepository,
    BestPractices,
    TeamNewsletter,
    VivaEngage
}
