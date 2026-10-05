namespace TeamIntelligenceHub.Application.DTOs;

/// <summary>
/// Payload for editing an Initiative. Same shape as creating one — every field the New
/// Initiative form collects is editable, including Owner.
/// </summary>
public class UpdateInitiativeRequestDto : CreateInitiativeRequestDto
{
}
