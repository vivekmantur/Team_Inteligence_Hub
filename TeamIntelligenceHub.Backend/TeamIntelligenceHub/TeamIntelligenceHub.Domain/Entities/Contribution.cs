using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Domain.Entities;

/// <summary>
/// Something worth capturing against an Initiative: an update, a deliverable, a metric,
/// a risk, a customer story, or several of those at once. Payload-free lists (Types, Tags,
/// ReuseTargets) are JSON columns; anything with a payload or foreign key stays a table.
/// </summary>
public class Contribution
{
    // Single source of truth for field limits, shared with the EF configuration and
    // the request DTO so a value can never pass validation only to fail at SQL.
    public const int TitleMinLength = 3;
    public const int TitleMaxLength = 200;
    public const int DescriptionMinLength = 10;
    public const int DescriptionMaxLength = 4000;
    public const int KeyTakeawayMaxLength = 500;
    public const int EnumValueMaxLength = 50;

    /// <summary>One tag. Long enough for a phrase, short enough to stay a label.</summary>
    public const int TagMaxLength = 50;

    // Ceilings for the three JSON columns, in characters of serialised array. Without an
    // explicit length EF emits nvarchar(max), which is stored off-row and reads slower.
    public const int TagsMaxLength = 1000;
    public const int TypesMaxLength = 200;
    public const int ReuseTargetsMaxLength = 400;

    /// <summary>At least one type must be chosen, matching the wizard's Next gate.</summary>
    public const int MinTypeCount = 1;

    public int Id { get; set; }

    public int InitiativeId { get; set; }

    /// <summary>
    /// Whoever holds the token at submission time. Never taken from the request body.
    /// </summary>
    public int SubmittedByUserId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    /// <summary>One sentence a leader could quote without rewriting it.</summary>
    public string? KeyTakeaway { get; set; }

    public ContributionPriority Priority { get; set; }

    public ContributionStatus Status { get; set; }

    /// <summary>
    /// What kinds of contribution this is. Stored as a JSON array of member names.
    /// Deduplicated by the service, because a JSON column cannot carry a unique index.
    /// </summary>
    public List<ContributionType> Types { get; set; } = new();

    /// <summary>
    /// Free-text keywords for cross-cutting themes the fixed type list cannot express.
    /// Trimmed and deduplicated by the service.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Which downstream surfaces may draw on this. Metadata only; nothing publishes
    /// automatically.
    /// </summary>
    public List<ContributionReuseTarget> ReuseTargets { get; set; } = new();

    /// <summary>Stamped once, when Status moves to Submitted. Null while a draft.</summary>
    public DateTime? SubmittedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties

    public Initiative Initiative { get; set; } = null!;

    public User SubmittedByUser { get; set; } = null!;

    /// <summary>Everyone credited, including the submitter.</summary>
    public ICollection<ContributionContributor> Contributors { get; set; }
        = new List<ContributionContributor>();

    /// <summary>Files uploaded as evidence.</summary>
    public ICollection<ContributionAttachment> Attachments { get; set; }
        = new List<ContributionAttachment>();

    /// <summary>Links to evidence held elsewhere.</summary>
    public ICollection<ContributionLink> Links { get; set; }
        = new List<ContributionLink>();

    // Conditional detail sections. Each is present only when the matching member is in
    // Types, which is why they are separate optional rows rather than nullable columns.

    public ContributionMetric? Metric { get; set; }

    public ContributionRisk? Risk { get; set; }

    public ContributionAiPractice? AiPractice { get; set; }

    public ContributionCustomerStory? CustomerStory { get; set; }

    public ContributionTestimonial? Testimonial { get; set; }
}
