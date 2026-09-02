using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Application.DTOs;

public class InitiativeResponseDto
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public InitiativeFocusArea BusinessArea { get; set; }

    public InitiativeWorkform InitiativeType { get; set; }

    public InitiativePriority Priority { get; set; }

    public int OwnerUserId { get; set; }

    public string? OwnerDisplayName { get; set; }

    public int? ExecutiveSponsorUserId { get; set; }

    public string? ExecutiveSponsorDisplayName { get; set; }

    public InitiativeSegment Segment { get; set; }

    public List<EnterpriseRole> ImpactedRoles { get; set; } = new();

    public InitiativeChangeImpact ChangeImpact { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly TargetEndDate { get; set; }

    public InitiativeLifecycleStage LifecycleStage { get; set; }

    public InitiativeHealth Health { get; set; }

    public InitiativeStatus Status { get; set; }

    public string? KeyObjective { get; set; }

    public string? ExpectedOutcome { get; set; }

    public string? SuccessMeasures { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
