using System.ComponentModel.DataAnnotations;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.DTOs;

/// <summary>
/// Payload for editing an existing membership. The person cannot be changed — remove
/// the membership and add another instead.
/// </summary>
public class UpdateInitiativeMemberRequestDto
{
    [Required(ErrorMessage = "Role is required.")]
    [StringLength(
        InitiativeMember.RoleMaxLength,
        MinimumLength = 2,
        ErrorMessage = "Role must be between {2} and {1} characters.")]
    public string Role { get; set; } = null!;

    [StringLength(InitiativeMember.ResponsibilityAreaMaxLength)]
    public string? ResponsibilityArea { get; set; }

    [Range(0, 100, ErrorMessage = "Allocation must be between 0 and 100 percent.")]
    public decimal? Allocation { get; set; }
}
