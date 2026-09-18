using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Domain.Entities;

/// <summary>
/// A testimonial or customer story extracted from a Contribution attachment's document
/// content, rather than typed in by hand through the wizard.
/// </summary>
/// <remarks>
/// Deliberately its own table rather than a row in ContributionTestimonials or
/// ContributionCustomerStories: those two share their primary key with Contribution
/// (at most one row per Contribution), but a Contribution's attachments can easily yield
/// several extracted rows. Own surrogate key here instead.
/// </remarks>
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

    /// <summary>Shared by both shapes — a quoted line of text.</summary>
    public string? Quote { get; set; }

    /// <summary>Customer name (CustomerStory) or speaker name (Testimonial).</summary>
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
