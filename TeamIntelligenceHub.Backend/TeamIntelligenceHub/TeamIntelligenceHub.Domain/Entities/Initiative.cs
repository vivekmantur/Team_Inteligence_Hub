using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Domain.Entities;

public class Initiative
{
    // Single source of truth for field limits, shared with the EF configuration and
    // the request DTO so a value can never pass validation only to fail at SQL.
    public const int NameMinLength = 3;
    public const int NameMaxLength = 255;
    public const int DescriptionMinLength = 10;
    public const int DescriptionMaxLength = 4000;
    public const int EnumValueMaxLength = 50;
    public const int LongTextMaxLength = 4000;

    /// <summary>Ceiling on serialised ImpactedRoles JSON, in characters — see Contribution.TagsMaxLength for why this is bounded rather than nvarchar(max).</summary>
    public const int ImpactedRolesMaxLength = 200;

    /// <summary>Longest span an Initiative may cover. Catches a mistyped year.</summary>
    public const int MaxDurationYears = 20;

    /// <summary>
    /// Floor for Start Date. Also catches an omitted date, which model binding would
    /// otherwise turn into DateOnly's default of 0001-01-01.
    /// </summary>
    public static readonly DateOnly EarliestStartDate = new(2000, 1, 1);

    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public InitiativeFocusArea BusinessArea { get; set; }

    public InitiativeWorkform InitiativeType { get; set; }

    public InitiativePriority Priority { get; set; }

    public int OwnerUserId { get; set; }

    public int? ExecutiveSponsorUserId { get; set; }

    public InitiativeSegment Segment { get; set; }

    /// <summary>Enterprise roles this Initiative's change lands on. Empty when none are flagged yet.</summary>
    public List<EnterpriseRole> ImpactedRoles { get; set; } = new();

    public InitiativeChangeImpact ChangeImpact { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly TargetEndDate { get; set; }

    /// <summary>Where in the change journey this Initiative sits.</summary>
    public InitiativeLifecycleStage LifecycleStage { get; set; }

    /// <summary>Whether execution needs attention, independent of lifecycle stage.</summary>
    public InitiativeHealth Health { get; set; }

    /// <summary>Whether work is active, paused, cancelled, or complete.</summary>
    public InitiativeStatus Status { get; set; }

    public string? KeyObjective { get; set; }

    public string? ExpectedOutcome { get; set; }

    public string? SuccessMeasures { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties

    public User Owner { get; set; } = null!;

    public User? ExecutiveSponsor { get; set; }

    /// <summary>The working team assigned to this Initiative.</summary>
    public ICollection<InitiativeMember> Members { get; set; }
        = new List<InitiativeMember>();

    /// <summary>Work items tracked under this Initiative.</summary>
    public ICollection<InitiativeTask> Tasks { get; set; }
        = new List<InitiativeTask>();

    /// <summary>Posts on this Initiative's activity feed.</summary>
    public ICollection<Activity> Activities { get; set; }
        = new List<Activity>();

    /// <summary>Contributions captured against this Initiative.</summary>
    public ICollection<Contribution> Contributions { get; set; }
        = new List<Contribution>();
}