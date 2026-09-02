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

    public DateTime JoinedAt { get; set; }
}
