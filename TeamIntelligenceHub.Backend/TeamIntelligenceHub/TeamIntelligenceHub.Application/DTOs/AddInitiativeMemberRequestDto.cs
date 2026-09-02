using System.ComponentModel.DataAnnotations;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.DTOs;

/// <summary>
/// Payload for adding a person to an Initiative's team.
/// </summary>
/// <remarks>
/// The Initiative is taken from the route, not the body, so a request cannot claim to
/// belong to one Initiative while being posted to another.
///
/// Role is free text. The UI offers a fixed list plus "Other" with a custom value, so
/// the caller sends the resolved string — "Legal Reviewer", never the literal "Other".
/// </remarks>
public class AddInitiativeMemberRequestDto
{
    [Required(ErrorMessage = "A team member is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Team member is not a valid user.")]
    public int? UserId { get; set; }

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
