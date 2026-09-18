using System.ComponentModel.DataAnnotations;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.DTOs;

public class UpdateAppRoleRequestDto
{
    [Required]
    [StringLength(User.AppRoleMaxLength, MinimumLength = 1)]
    public string AppRole { get; set; } = null!;
}
