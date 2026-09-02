namespace TeamIntelligenceHub.Application.DTOs;

public class UserResponseDto
{
    public int Id { get; set; }

    public string EntraObjectId { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }
}