namespace TeamIntelligenceHub.Domain.Enums;

/// <summary>
/// A downstream surface that may draw on a contribution.
/// </summary>
/// <remarks>
/// Metadata, not access control. Selecting a target does not publish anything; it records
/// the author's consent for that surface to use the material. Distinct from
/// InitiativeVisibility, which does govern who can see an Initiative.
/// </remarks>
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
