namespace TeamIntelligenceHub.Domain.Entities;

/// <summary>
/// A measurable business result. Present only when the contribution is a Business Metric.
/// </summary>
/// <remarks>
/// ContributionId is both the primary key and the foreign key, a shared primary key. That
/// makes the 1:1 real: the row cannot exist without its contribution, and there cannot be
/// two of them.
///
/// Previous and current are decimal rather than the free text the wizard collects today,
/// so Analytics can compute the delta instead of only displaying it.
/// </remarks>
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
