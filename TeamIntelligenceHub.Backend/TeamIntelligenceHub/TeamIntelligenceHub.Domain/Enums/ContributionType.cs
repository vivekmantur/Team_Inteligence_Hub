namespace TeamIntelligenceHub.Domain.Enums;

/// <summary>
/// What kind of thing a contribution is. A contribution may be several at once. Each
/// member enables its matching detail table, such as BusinessMetric for ContributionMetrics.
/// </summary>
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
