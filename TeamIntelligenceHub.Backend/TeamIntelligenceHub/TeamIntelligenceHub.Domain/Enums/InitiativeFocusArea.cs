namespace TeamIntelligenceHub.Domain.Enums;

/// <summary>
/// The area of work an Initiative primarily supports. A closed set rather than free
/// text, so the value can be filtered and reported on consistently.
/// </summary>
public enum InitiativeFocusArea
{
    AiTransformation,
    ChangeManagementAndAdoption,
    Enablement,
    InsightsAndMeasurement,
    StorytellingAndEvidence,
    StrategicPrograms,
    Other
}
