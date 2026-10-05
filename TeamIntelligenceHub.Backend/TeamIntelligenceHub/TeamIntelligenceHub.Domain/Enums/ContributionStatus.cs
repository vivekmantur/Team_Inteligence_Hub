namespace TeamIntelligenceHub.Domain.Enums;

/// <summary>
/// Where a contribution is in its short lifecycle. Submitted makes it part of the
/// Initiative's record and stamps SubmittedAt.
/// </summary>
public enum ContributionStatus
{
    Draft,
    Submitted
}
