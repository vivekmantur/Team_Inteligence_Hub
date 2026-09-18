using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Domain.Entities;

/// <summary>
/// A quote from a stakeholder or customer. Present only when the contribution is a
/// Testimonial.
/// </summary>
/// <remarks>
/// Shared primary key with Contribution, like the other conditional detail tables.
/// </remarks>
public class ContributionTestimonial
{
    public const int QuoteMaxLength = 2000;
    public const int SpeakerNameMaxLength = 200;
    public const int SpeakerRoleMaxLength = 200;
    public const int EnumValueMaxLength = 50;

    public int ContributionId { get; set; }

    /// <summary>Said by a stakeholder or customer, in their words.</summary>
    public string Quote { get; set; } = null!;

    public string SpeakerName { get; set; } = null!;

    public string? SpeakerRole { get; set; }

    public TestimonialAudience Audience { get; set; }

    public TestimonialSentiment Sentiment { get; set; }

    // Navigation properties

    public Contribution Contribution { get; set; } = null!;
}
