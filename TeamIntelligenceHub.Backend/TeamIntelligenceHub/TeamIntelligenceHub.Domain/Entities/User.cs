namespace TeamIntelligenceHub.Domain.Entities;

public class User
{
    // Single source of truth for field limits. The EF configuration and the DTO
    // annotations both read these, so the three cannot drift apart.
    public const int EntraObjectIdMaxLength = 100;
    public const int EmailMaxLength = 255;
    public const int DisplayNameMaxLength = 255;
    public const int AppRoleMaxLength = 100;

    /// <summary>Assigned to every user on creation; free text after that, not an enum.</summary>
    public const string DefaultAppRole = "Contributor";

    public int Id { get; set; }

    public string EntraObjectId { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// This person's role/title within the app (e.g. shown on the Team page's Skills &amp;
    /// Expertise panel). Free text a user sets for themselves, not a permissions level.
    /// </summary>
    public string AppRole { get; set; } = DefaultAppRole;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    // Navigation properties

    public ICollection<Initiative> OwnedInitiatives { get; set; }
        = new List<Initiative>();

    public ICollection<Initiative> SponsoredInitiatives { get; set; }
        = new List<Initiative>();

    /// <summary>Initiatives this person is a team member of.</summary>
    public ICollection<InitiativeMember> InitiativeMemberships { get; set; }
        = new List<InitiativeMember>();

    /// <summary>Tasks currently assigned to this person.</summary>
    public ICollection<InitiativeTask> AssignedTasks { get; set; }
        = new List<InitiativeTask>();

    /// <summary>Tasks this person raised.</summary>
    public ICollection<InitiativeTask> CreatedTasks { get; set; }
        = new List<InitiativeTask>();

    /// <summary>Comments this person wrote.</summary>
    public ICollection<TaskComment> TaskComments { get; set; }
        = new List<TaskComment>();

    /// <summary>Comments that mention this person.</summary>
    public ICollection<TaskCommentMention> TaskCommentMentions { get; set; }
        = new List<TaskCommentMention>();

    /// <summary>Activity posts this person wrote.</summary>
    public ICollection<Activity> Activities { get; set; }
        = new List<Activity>();

    /// <summary>Activity posts that mention this person.</summary>
    public ICollection<ActivityMention> ActivityMentions { get; set; }
        = new List<ActivityMention>();

    /// <summary>Contributions this person submitted.</summary>
    public ICollection<Contribution> SubmittedContributions { get; set; }
        = new List<Contribution>();

    /// <summary>Contributions this person is credited on.</summary>
    public ICollection<ContributionContributor> ContributionCredits { get; set; }
        = new List<ContributionContributor>();

    /// <summary>Risks this person owns.</summary>
    public ICollection<ContributionRisk> OwnedContributionRisks { get; set; }
        = new List<ContributionRisk>();
}