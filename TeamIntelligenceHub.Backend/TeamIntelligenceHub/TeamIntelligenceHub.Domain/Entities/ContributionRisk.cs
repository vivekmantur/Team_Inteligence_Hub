using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Domain.Entities;

/// <summary>
/// Something that needs attention. Present only when the contribution is a Risk.
/// </summary>
/// <remarks>
/// Shared primary key with Contribution, so the row cannot outlive it.
///
/// The owner is a real user rather than the typed-in name the wizard collects today,
/// which turns "risks I own" into a query instead of a string match. Nullable so an
/// unassigned risk can still be recorded.
/// </remarks>
public class ContributionRisk
{
    public const int DescriptionMaxLength = 2000;
    public const int BusinessImpactMaxLength = 2000;
    public const int MitigationMaxLength = 2000;
    public const int SupportNeededMaxLength = 500;
    public const int EnumValueMaxLength = 50;

    public int ContributionId { get; set; }

    public string Description { get; set; } = null!;

    public RiskSeverity Severity { get; set; }

    public string? BusinessImpact { get; set; }

    public string? Mitigation { get; set; }

    public string? SupportNeeded { get; set; }

    /// <summary>Who is accountable. Null when nobody has taken it yet.</summary>
    public int? OwnerUserId { get; set; }

    /// <summary>A date rather than free text, so overdue risks can be found.</summary>
    public DateOnly? TargetResolutionDate { get; set; }

    // Navigation properties

    public Contribution Contribution { get; set; } = null!;

    public User? OwnerUser { get; set; }
}
