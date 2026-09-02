namespace TeamIntelligenceHub.Domain.Enums;

/// <summary>
/// Operational state of an Initiative — whether work is happening at all. Distinct
/// from <see cref="InitiativeLifecycleStage"/> (where in the change journey) and
/// <see cref="InitiativeHealth"/> (whether execution needs attention), which used to
/// be folded into this single field. Display labels ("On Hold") live in the UI —
/// these names are the stored and transmitted identifiers.
/// </summary>
public enum InitiativeStatus
{
    Active,
    OnHold,
    Cancelled,
    Completed
}
