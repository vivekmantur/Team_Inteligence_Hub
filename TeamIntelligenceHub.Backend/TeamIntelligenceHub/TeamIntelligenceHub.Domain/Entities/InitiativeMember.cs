namespace TeamIntelligenceHub.Domain.Entities;

/// <summary>
/// A person assigned to an Initiative, with the part they play on it.
/// </summary>
/// <remarks>
/// Distinct from Initiative.Owner and Initiative.ExecutiveSponsor, which are single
/// columns on the Initiative itself. This is the working team, and a person can appear
/// on many Initiatives.
/// </remarks>
public class InitiativeMember
{
    // Single source of truth for field limits, shared with the EF configuration and
    // the request DTO so a value can never pass validation only to fail at SQL.
    public const int RoleMaxLength = 100;
    public const int ResponsibilityAreaMaxLength = 255;

    /// <summary>Percentage of a person's time. Stored as decimal(5,2).</summary>
    public const decimal MinAllocationPercent = 0m;
    public const decimal MaxAllocationPercent = 100m;

    public int Id { get; set; }

    public int InitiativeId { get; set; }

    public int UserId { get; set; }

    /// <summary>
    /// Free text rather than an enum. The UI offers a fixed list but includes "Other"
    /// with a custom value, so the set is deliberately open.
    /// </summary>
    public string Role { get; set; } = null!;

    public string? ResponsibilityArea { get; set; }

    /// <summary>Percentage allocation, 0 to 100. Null when not tracked.</summary>
    public decimal? Allocation { get; set; }

    public DateTime JoinedAt { get; set; }

    // Navigation properties

    public Initiative Initiative { get; set; } = null!;

    public User User { get; set; } = null!;
}
