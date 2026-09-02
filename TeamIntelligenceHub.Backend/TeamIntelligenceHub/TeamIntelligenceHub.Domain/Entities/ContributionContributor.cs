namespace TeamIntelligenceHub.Domain.Entities;

/// <summary>
/// Somebody credited on a contribution, and what they did on it.
/// </summary>
/// <remarks>
/// Stays a table rather than a JSON list because each row carries a payload and a foreign
/// key to Users. "Contributions by person" is what the Team Contributions page reads.
/// </remarks>
public class ContributionContributor
{
    public const int ResponsibilityAreaMaxLength = 255;

    public int Id { get; set; }

    public int ContributionId { get; set; }

    public int UserId { get; set; }

    /// <summary>
    /// What this person covered. Same limit as InitiativeMember.ResponsibilityArea, whose
    /// vocabulary the wizard reuses.
    /// </summary>
    public string? ResponsibilityArea { get; set; }

    /// <summary>
    /// The Primary / Supporting toggle. Not exclusive: several people may be primary,
    /// which is why this is a flag rather than a single PrimaryContributorId column.
    /// </summary>
    public bool IsPrimary { get; set; }

    public DateTime AddedAt { get; set; }

    // Navigation properties

    public Contribution Contribution { get; set; } = null!;

    public User User { get; set; } = null!;
}
