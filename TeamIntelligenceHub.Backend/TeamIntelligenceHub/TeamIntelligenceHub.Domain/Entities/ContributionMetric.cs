namespace TeamIntelligenceHub.Domain.Entities;

/// <summary>
/// A measurable business result. Present only when the contribution is a Business Metric.
/// It shares its primary key with Contribution, so there is at most one per contribution.
/// </summary>
public class ContributionMetric
{
    public const int MetricNameMaxLength = 150;
    public const int UnitMaxLength = 50;
    public const int ReportingPeriodMaxLength = 50;

    public int ContributionId { get; set; }

    public string MetricName { get; set; } = null!;

    /// <summary>users, %, hours. Free text; the set is open by nature.</summary>
    public string? Unit { get; set; }

    public decimal? PreviousValue { get; set; }

    public decimal? CurrentValue { get; set; }

    /// <summary>Free text such as "Q3 FY26". Fiscal calendars vary too much to model.</summary>
    public string? ReportingPeriod { get; set; }

    // Navigation properties

    public Contribution Contribution { get; set; } = null!;
}
