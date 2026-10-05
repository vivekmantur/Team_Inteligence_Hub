namespace TeamIntelligenceHub.Domain.Entities;

/// <summary>
/// A repeatable AI or Copilot pattern worth sharing. Present only when the contribution
/// is an AI Best Practice. Time saved is numeric hours per week, so it can be summed.
/// </summary>
public class ContributionAiPractice
{
    public const int ToolMaxLength = 100;
    public const int UseCaseMaxLength = 2000;
    public const int RecommendationMaxLength = 2000;

    /// <summary>Sanity ceiling. Nobody saves more hours per week than a week holds.</summary>
    public const decimal MaxTimeSavedHoursPerWeek = 168m;

    public int ContributionId { get; set; }

    /// <summary>Free text. New tools appear faster than an enum could track them.</summary>
    public string Tool { get; set; } = null!;

    public string? UseCase { get; set; }

    /// <summary>
    /// Pasted verbatim so others can reuse it. Unbounded in SQL: any ceiling here would
    /// be a guess, and a truncated prompt is worse than no prompt.
    /// </summary>
    public string? Prompt { get; set; }

    public decimal? TimeSavedHoursPerWeek { get; set; }

    public string? Recommendation { get; set; }

    // Navigation properties

    public Contribution Contribution { get; set; } = null!;
}
