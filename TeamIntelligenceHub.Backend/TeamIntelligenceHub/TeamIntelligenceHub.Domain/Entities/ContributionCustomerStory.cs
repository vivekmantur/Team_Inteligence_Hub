namespace TeamIntelligenceHub.Domain.Entities;

/// <summary>
/// A real customer outcome. Present only when the contribution is a Customer Story.
/// </summary>
/// <remarks>
/// Shared primary key with Contribution. Quote is what the Testimonials page reads, which
/// is why it is its own column rather than part of Summary.
/// </remarks>
public class ContributionCustomerStory
{
    public const int CustomerNameMaxLength = 200;
    public const int SummaryMaxLength = 4000;
    public const int OutcomeMaxLength = 300;
    public const int QuoteMaxLength = 2000;
    public const int BusinessValueMaxLength = 500;

    public int ContributionId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string? Summary { get; set; }

    /// <summary>The headline result, such as "43% faster onboarding".</summary>
    public string? Outcome { get; set; }

    /// <summary>Said by a stakeholder or customer, in their words.</summary>
    public string? Quote { get; set; }

    public string? BusinessValue { get; set; }

    // Navigation properties

    public Contribution Contribution { get; set; } = null!;
}
