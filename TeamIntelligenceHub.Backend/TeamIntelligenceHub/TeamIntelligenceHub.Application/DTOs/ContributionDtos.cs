using System.ComponentModel.DataAnnotations;
using TeamIntelligenceHub.Domain.Entities;
using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Application.DTOs;

// ---------------------------------------------------------------------------
// Requests
// ---------------------------------------------------------------------------

/// <summary>
/// Payload for creating a contribution. Carries the whole graph, because the wizard
/// submits all eight of its steps at once.
/// </summary>
/// <remarks>
/// The Initiative comes from the route and the submitter from the bearer token, so
/// neither can be spoofed by the body. Attachments are not here: a file needs a
/// ContributionId to hang off, so it is uploaded after this call returns.
/// </remarks>
public class CreateContributionRequestDto
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(
        Contribution.TitleMaxLength,
        MinimumLength = Contribution.TitleMinLength,
        ErrorMessage = "Title must be between {2} and {1} characters.")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(
        Contribution.DescriptionMaxLength,
        MinimumLength = Contribution.DescriptionMinLength,
        ErrorMessage = "Description must be between {2} and {1} characters.")]
    public string Description { get; set; } = null!;

    [StringLength(
        Contribution.KeyTakeawayMaxLength,
        ErrorMessage = "Key takeaway cannot exceed {1} characters.")]
    public string? KeyTakeaway { get; set; }

    /// <summary>Defaults to Medium when omitted. An unknown value is a 400.</summary>
    public ContributionPriority? Priority { get; set; }

    /// <summary>
    /// Defaults to Draft when omitted, which is what the wizard's Save draft button
    /// sends. Submitted stamps SubmittedAt.
    /// </summary>
    public ContributionStatus? Status { get; set; }

    /// <summary>At least one is required. Duplicates are collapsed.</summary>
    [Required(ErrorMessage = "Pick at least one contribution type.")]
    [MinLength(
        Contribution.MinTypeCount,
        ErrorMessage = "Pick at least one contribution type.")]
    public List<ContributionType> Types { get; set; } = [];

    /// <summary>Trimmed, emptied of blanks, and deduplicated before storage.</summary>
    public List<string>? Tags { get; set; }

    public List<ContributionReuseTarget>? ReuseTargets { get; set; }

    /// <summary>
    /// Everyone credited. The submitter is added automatically if left out, so the
    /// client does not have to remember to include themselves.
    /// </summary>
    public List<ContributionContributorRequestDto>? Contributors { get; set; }

    public List<ContributionLinkRequestDto>? Links { get; set; }

    // Detail sections. Each may only be supplied when the matching member is in Types;
    // sending a metric on a Progress Update is a 400 rather than a silently orphaned row.

    public ContributionMetricRequestDto? Metric { get; set; }

    public ContributionRiskRequestDto? Risk { get; set; }

    public ContributionAiPracticeRequestDto? AiPractice { get; set; }

    public ContributionCustomerStoryRequestDto? CustomerStory { get; set; }

    public ContributionTestimonialRequestDto? Testimonial { get; set; }
}

/// <summary>
/// Payload for editing a contribution. Replaces the whole graph, matching the wizard,
/// which reopens every step rather than patching one field.
/// </summary>
/// <remarks>
/// Attachments are excluded and managed through their own endpoints, the same split
/// task comments use.
/// </remarks>
public class UpdateContributionRequestDto : CreateContributionRequestDto
{
}

public class ContributionContributorRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Contributor is not a valid user.")]
    public int UserId { get; set; }

    [StringLength(
        ContributionContributor.ResponsibilityAreaMaxLength,
        ErrorMessage = "Responsibility area cannot exceed {1} characters.")]
    public string? ResponsibilityArea { get; set; }

    /// <summary>Not exclusive. Several people may be primary on one contribution.</summary>
    public bool IsPrimary { get; set; }
}

public class ContributionLinkRequestDto
{
    public ContributionLinkSource Source { get; set; }

    [Required(ErrorMessage = "A link URL is required.")]
    [StringLength(
        ContributionLink.UrlMaxLength,
        ErrorMessage = "Link URL cannot exceed {1} characters.")]
    public string Url { get; set; } = null!;

    [StringLength(
        ContributionLink.LabelMaxLength,
        ErrorMessage = "Link label cannot exceed {1} characters.")]
    public string? Label { get; set; }

    [StringLength(
        ContributionLink.DescriptionMaxLength,
        ErrorMessage = "Link description cannot exceed {1} characters.")]
    public string? Description { get; set; }
}

public class ContributionMetricRequestDto
{
    [Required(ErrorMessage = "Metric name is required.")]
    [StringLength(
        ContributionMetric.MetricNameMaxLength,
        ErrorMessage = "Metric name cannot exceed {1} characters.")]
    public string MetricName { get; set; } = null!;

    [StringLength(
        ContributionMetric.UnitMaxLength,
        ErrorMessage = "Unit cannot exceed {1} characters.")]
    public string? Unit { get; set; }

    /// <summary>Negative is valid. A metric is allowed to have gone down.</summary>
    public decimal? PreviousValue { get; set; }

    public decimal? CurrentValue { get; set; }

    [StringLength(
        ContributionMetric.ReportingPeriodMaxLength,
        ErrorMessage = "Reporting period cannot exceed {1} characters.")]
    public string? ReportingPeriod { get; set; }
}

public class ContributionRiskRequestDto
{
    [Required(ErrorMessage = "Risk description is required.")]
    [StringLength(
        ContributionRisk.DescriptionMaxLength,
        ErrorMessage = "Risk description cannot exceed {1} characters.")]
    public string Description { get; set; } = null!;

    /// <summary>Defaults to Medium when omitted.</summary>
    public RiskSeverity? Severity { get; set; }

    [StringLength(ContributionRisk.BusinessImpactMaxLength)]
    public string? BusinessImpact { get; set; }

    [StringLength(ContributionRisk.MitigationMaxLength)]
    public string? Mitigation { get; set; }

    [StringLength(ContributionRisk.SupportNeededMaxLength)]
    public string? SupportNeeded { get; set; }

    /// <summary>Null leaves the risk unowned.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Risk owner is not a valid user.")]
    public int? OwnerUserId { get; set; }

    public DateOnly? TargetResolutionDate { get; set; }
}

public class ContributionAiPracticeRequestDto
{
    [Required(ErrorMessage = "AI tool is required.")]
    [StringLength(
        ContributionAiPractice.ToolMaxLength,
        ErrorMessage = "AI tool cannot exceed {1} characters.")]
    public string Tool { get; set; } = null!;

    [StringLength(ContributionAiPractice.UseCaseMaxLength)]
    public string? UseCase { get; set; }

    /// <summary>No length cap. A truncated prompt is worse than no prompt.</summary>
    public string? Prompt { get; set; }

    public decimal? TimeSavedHoursPerWeek { get; set; }

    [StringLength(ContributionAiPractice.RecommendationMaxLength)]
    public string? Recommendation { get; set; }
}

public class ContributionCustomerStoryRequestDto
{
    [Required(ErrorMessage = "Customer name is required.")]
    [StringLength(
        ContributionCustomerStory.CustomerNameMaxLength,
        ErrorMessage = "Customer name cannot exceed {1} characters.")]
    public string CustomerName { get; set; } = null!;

    [StringLength(ContributionCustomerStory.SummaryMaxLength)]
    public string? Summary { get; set; }

    [StringLength(ContributionCustomerStory.OutcomeMaxLength)]
    public string? Outcome { get; set; }

    [StringLength(ContributionCustomerStory.QuoteMaxLength)]
    public string? Quote { get; set; }

    [StringLength(ContributionCustomerStory.BusinessValueMaxLength)]
    public string? BusinessValue { get; set; }
}

public class ContributionTestimonialRequestDto
{
    [Required(ErrorMessage = "Quote is required.")]
    [StringLength(
        ContributionTestimonial.QuoteMaxLength,
        ErrorMessage = "Quote cannot exceed {1} characters.")]
    public string Quote { get; set; } = null!;

    [Required(ErrorMessage = "Speaker name is required.")]
    [StringLength(
        ContributionTestimonial.SpeakerNameMaxLength,
        ErrorMessage = "Speaker name cannot exceed {1} characters.")]
    public string SpeakerName { get; set; } = null!;

    [StringLength(ContributionTestimonial.SpeakerRoleMaxLength)]
    public string? SpeakerRole { get; set; }

    /// <summary>Defaults to Stakeholder when omitted.</summary>
    public TestimonialAudience? Audience { get; set; }

    /// <summary>Defaults to Positive when omitted.</summary>
    public TestimonialSentiment? Sentiment { get; set; }
}

// ---------------------------------------------------------------------------
// Responses
// ---------------------------------------------------------------------------

public class ContributionResponseDto
{
    public int Id { get; set; }

    public int InitiativeId { get; set; }

    /// <summary>
    /// Projected from the Initiative rather than stored, so renaming an Initiative
    /// cannot leave a stale copy behind on every contribution.
    /// </summary>
    public string InitiativeName { get; set; } = null!;

    public string Workstream { get; set; } = null!;

    public int SubmittedByUserId { get; set; }

    public string SubmittedByDisplayName { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? KeyTakeaway { get; set; }

    public ContributionPriority Priority { get; set; }

    public ContributionStatus Status { get; set; }

    public List<ContributionType> Types { get; set; } = [];

    public List<string> Tags { get; set; } = [];

    public List<ContributionReuseTarget> ReuseTargets { get; set; } = [];

    public DateTime? SubmittedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public List<ContributionContributorDto> Contributors { get; set; } = [];

    public List<ContributionLinkDto> Links { get; set; } = [];

    public List<ContributionAttachmentDto> Attachments { get; set; } = [];

    public ContributionMetricDto? Metric { get; set; }

    public ContributionRiskDto? Risk { get; set; }

    public ContributionAiPracticeDto? AiPractice { get; set; }

    public ContributionCustomerStoryDto? CustomerStory { get; set; }

    public ContributionTestimonialDto? Testimonial { get; set; }
}

public class ContributionContributorDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string DisplayName { get; set; } = null!;

    public string? ResponsibilityArea { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime AddedAt { get; set; }
}

public class ContributionLinkDto
{
    public int Id { get; set; }

    public ContributionLinkSource Source { get; set; }

    public string Url { get; set; } = null!;

    public string? Label { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class ContributionAttachmentDto
{
    public int Id { get; set; }

    public int ContributionId { get; set; }

    public string FileName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    /// <summary>Bytes. The client formats this for display.</summary>
    public long FileSize { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class ContributionMetricDto
{
    public string MetricName { get; set; } = null!;

    public string? Unit { get; set; }

    public decimal? PreviousValue { get; set; }

    public decimal? CurrentValue { get; set; }

    public string? ReportingPeriod { get; set; }
}

public class ContributionRiskDto
{
    public string Description { get; set; } = null!;

    public RiskSeverity Severity { get; set; }

    public string? BusinessImpact { get; set; }

    public string? Mitigation { get; set; }

    public string? SupportNeeded { get; set; }

    public int? OwnerUserId { get; set; }

    public string? OwnerDisplayName { get; set; }

    public DateOnly? TargetResolutionDate { get; set; }
}

public class ContributionAiPracticeDto
{
    public string Tool { get; set; } = null!;

    public string? UseCase { get; set; }

    public string? Prompt { get; set; }

    public decimal? TimeSavedHoursPerWeek { get; set; }

    public string? Recommendation { get; set; }
}

public class ContributionCustomerStoryDto
{
    public string CustomerName { get; set; } = null!;

    public string? Summary { get; set; }

    public string? Outcome { get; set; }

    public string? Quote { get; set; }

    public string? BusinessValue { get; set; }
}

public class ContributionTestimonialDto
{
    public string Quote { get; set; } = null!;

    public string SpeakerName { get; set; } = null!;

    public string? SpeakerRole { get; set; }

    public TestimonialAudience Audience { get; set; }

    public TestimonialSentiment Sentiment { get; set; }
}

/// <summary>
/// One card on the Stories &amp; Evidence page. Slimmer than ContributionResponseDto: a
/// showcase grid has no use for contributors, links, attachments, or the other four
/// detail sections, so this only carries what the card renders.
/// </summary>
public class CustomerStoryCardDto
{
    public int Id { get; set; }

    public int InitiativeId { get; set; }

    public string InitiativeName { get; set; } = null!;

    public int SubmittedByUserId { get; set; }

    public string Title { get; set; } = null!;

    public string? KeyTakeaway { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public string CustomerName { get; set; } = null!;

    public string? Summary { get; set; }

    public string? Outcome { get; set; }

    public string? Quote { get; set; }

    public string? BusinessValue { get; set; }
}

/// <summary>
/// One card on the Stories &amp; Evidence page's Testimonial tab. Slimmer than
/// ContributionResponseDto, on the same reasoning as CustomerStoryCardDto.
/// </summary>
public class TestimonialCardDto
{
    public int Id { get; set; }

    public int InitiativeId { get; set; }

    public string InitiativeName { get; set; } = null!;

    public int SubmittedByUserId { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public string Quote { get; set; } = null!;

    public string SpeakerName { get; set; } = null!;

    public string? SpeakerRole { get; set; }

    public TestimonialAudience Audience { get; set; }

    public TestimonialSentiment Sentiment { get; set; }
}

/// <summary>
/// One card on the Stories &amp; Evidence page's "Extracted from documents" section,
/// customer-story half. Sourced from DocumentTestimonialsAndCustomerStories rather than a
/// Contribution's own CustomerStory row, so it carries the source attachment's file name
/// instead of a contributor-written Title.
/// </summary>
public class DocumentCustomerStoryCardDto
{
    public int Id { get; set; }

    public int ContributionId { get; set; }

    public int InitiativeId { get; set; }

    public string InitiativeName { get; set; } = null!;

    public string SourceFileName { get; set; } = null!;

    public string? CustomerName { get; set; }

    public string? Summary { get; set; }

    public string? Outcome { get; set; }

    public string? Quote { get; set; }

    public string? BusinessValue { get; set; }
}

/// <summary>
/// One card on the Stories &amp; Evidence page's "Extracted from documents" section,
/// testimonial half. See DocumentCustomerStoryCardDto for why this is separate from
/// TestimonialCardDto.
/// </summary>
public class DocumentTestimonialCardDto
{
    public int Id { get; set; }

    public int ContributionId { get; set; }

    public int InitiativeId { get; set; }

    public string InitiativeName { get; set; } = null!;

    public string SourceFileName { get; set; } = null!;

    public string? Quote { get; set; }

    public string? SpeakerName { get; set; }

    public string? SpeakerRole { get; set; }

    public TestimonialAudience? Audience { get; set; }

    public TestimonialSentiment? Sentiment { get; set; }
}
