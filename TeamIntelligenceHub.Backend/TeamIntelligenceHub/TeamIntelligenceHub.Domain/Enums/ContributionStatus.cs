namespace TeamIntelligenceHub.Domain.Enums;

/// <summary>
/// Where a contribution is in its short lifecycle.
/// </summary>
/// <remarks>
/// A draft is the author's own working copy. Submitted is the point at which it becomes
/// part of the Initiative's record, and the point at which SubmittedAt is stamped.
/// </remarks>
public enum ContributionStatus
{
    Draft,
    Submitted
}
