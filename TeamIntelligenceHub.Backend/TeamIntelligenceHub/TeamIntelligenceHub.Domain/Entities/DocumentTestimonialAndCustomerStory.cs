using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Domain.Entities;

/// <summary>
/// A testimonial or customer story extracted from a Contribution attachment's document
/// content, rather than typed in by hand through the wizard. It has its own table and key
/// because one Contribution's attachments can yield several rows.
/// </summary>
public class DocumentTestimonialAndCustomerStory
{
    public const int NameMaxLength = 200;
    public const int RoleMaxLength = 200;
    public const int QuoteMaxLength = 2000;
    public const int SummaryMaxLength = 4000;
    public const int OutcomeMaxLength = 300;
    public const int BusinessValueMaxLength = 500;
    public const int EnumValueMaxLength = 50;

    public int Id { get; set; }

    public int ContributionAttachmentId { get; set; }

    public DocumentInsightType Type { get; set; }

    /// <summary>A quoted line of text. Used by both shapes.</summary>
    public string? Quote { get; set; }

    /// <summary>The customer's name. CustomerStory only; testimonials use SpeakerName.</summary>
    public string? CustomerName { get; set; }

    public string? Summary { get; set; }

    public string? Outcome { get; set; }

    public string? BusinessValue { get; set; }

    public string? SpeakerName { get; set; }

    public string? SpeakerRole { get; set; }

    public TestimonialAudience? Audience { get; set; }

    public TestimonialSentiment? Sentiment { get; set; }

    // Navigation properties

    public ContributionAttachment ContributionAttachment { get; set; } = null!;
}
