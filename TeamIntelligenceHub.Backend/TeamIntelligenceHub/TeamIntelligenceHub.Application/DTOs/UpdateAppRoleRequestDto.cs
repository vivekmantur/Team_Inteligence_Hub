using System.ComponentModel.DataAnnotations;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.DTOs;

/// <summary>Payload for setting the caller's own AppRole.</summary>
public class UpdateAppRoleRequestDto
{
    [Required]
    [StringLength(User.AppRoleMaxLength, MinimumLength = 1)]
    public string AppRole { get; set; } = null!;
}
