namespace TeamIntelligenceHub.Domain.Enums;

/// <summary>
/// What kind of thing a contribution is. A contribution may be several at once.
/// </summary>
/// <remarks>
/// Selecting a member here is what makes the matching detail table applicable:
/// BusinessMetric enables ContributionMetrics, Risk enables ContributionRisks, and so on.
/// Closed set on purpose. The wizard offers an explicit "Other" card, so there is no
/// need for a free-text escape hatch.
/// </remarks>
public enum ContributionType
{
    ProgressUpdate,
    Deliverable,
    CustomerStory,
    BusinessMetric,
    Risk,
    Decision,
    AiBestPractice,
    Testimonial,
    SupportingAsset,
    SupportNeeded,
    Other
}
