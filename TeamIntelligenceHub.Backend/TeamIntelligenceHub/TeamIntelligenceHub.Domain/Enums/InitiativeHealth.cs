namespace TeamIntelligenceHub.Domain.Enums;

/// <summary>Whether an Initiative's execution needs attention, independent of its lifecycle stage.</summary>
public enum InitiativeHealth
{
    OnTrack,
    NeedsAttention,
    AtRisk
}
