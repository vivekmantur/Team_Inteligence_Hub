namespace TeamIntelligenceHub.Application.DTOs;

public class InitiativeMemberResponseDto
{
    public int Id { get; set; }

    public int InitiativeId { get; set; }

    public int UserId { get; set; }

    public string UserDisplayName { get; set; } = null!;

    public string UserEmail { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string? ResponsibilityArea { get; set; }

    public decimal? Allocation { get; set; }

    /// <summary>
    /// This person's Allocation summed across every Initiative they belong to, not just
    /// this one — untracked rows count as 0, never as unknown.
    /// </summary>
    public decimal TotalAllocationAcrossInitiatives { get; set; }

    public DateTime JoinedAt { get; set; }
}
