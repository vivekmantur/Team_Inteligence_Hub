namespace TeamIntelligenceHub.Application.DTOs;

/// <summary>
/// What deleting an Initiative would also remove. Read before the delete itself, so the
/// client can show a confirmation naming real counts rather than a generic warning.
/// </summary>
public class InitiativeDeletionImpactDto
{
    public int ContributionCount { get; set; }

    public int TaskCount { get; set; }

    public int ActivityCount { get; set; }

    public int TeamMemberCount { get; set; }
}
