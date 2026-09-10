using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Application.Services.ContentGeneration;

/// <summary>
/// Everything a prompt for one Content Studio generation is allowed to see, already
/// translated out of EF entities. Every format populates only the fields its own context
/// rule names — the rest stay at their empty default, which is what makes "this format
/// used only its specified data" a fact about the object rather than a convention callers
/// have to remember.
/// </summary>
/// <remarks>
/// Deliberately carries no Contributor, Link, or Attachment data — there is no property
/// here to hold any of it, so a future change that tried to add it would show up as a
/// new field on this type, not a silent leak.
/// </remarks>
public sealed class ContentGenerationContext
{
    public IReadOnlyList<MetricContext> Metrics { get; init; } = [];

    /// <summary>Just the quotable line and who said it — LinkedInPost's "CustomerStory.Quote".</summary>
    public IReadOnlyList<CustomerQuoteContext> CustomerQuotes { get; init; } = [];

    /// <summary>The full customer-story backbone — Blog and CaseStudy.</summary>
    public IReadOnlyList<CustomerStoryContext> CustomerStories { get; init; } = [];

    public IReadOnlyList<RiskContext> Risks { get; init; } = [];

    public IReadOnlyList<AiPracticeContext> AiPractices { get; init; } = [];

    /// <summary>Pulled straight from Contribution.KeyTakeaway, not any detail table.</summary>
    public IReadOnlyList<string> KeyTakeaways { get; init; } = [];

    /// <summary>
    /// Newsletter and Blog's base-field narrative — Title/Description/KeyTakeaway off
    /// Contributions typed Deliverable or ProgressUpdate, which have no detail table of
    /// their own.
    /// </summary>
    public IReadOnlyList<ContributionSummaryContext> ContributionSummaries { get; init; } = [];

    /// <summary>QbrSlide's roadmap fields. Live on Initiative, not on any Contribution.</summary>
    public string? InitiativeExpectedOutcome { get; init; }

    public string? InitiativeSuccessMeasures { get; init; }

    public string? InitiativeKeyObjective { get; init; }
}

public sealed class MetricContext
{
    public string MetricName { get; init; } = null!;

    public string? Unit { get; init; }

    public decimal? PreviousValue { get; init; }

    public decimal? CurrentValue { get; init; }
}

public sealed class CustomerQuoteContext
{
    public string CustomerName { get; init; } = null!;

    public string? Quote { get; init; }
}

public sealed class CustomerStoryContext
{
    public string CustomerName { get; init; } = null!;

    public string? Summary { get; init; }

    public string? Outcome { get; init; }

    public string? Quote { get; init; }

    public string? BusinessValue { get; init; }
}

public sealed class RiskContext
{
    public string Description { get; init; } = null!;

    public RiskSeverity Severity { get; init; }

    public string? BusinessImpact { get; init; }
}

public sealed class AiPracticeContext
{
    public string Tool { get; init; } = null!;

    public string? UseCase { get; init; }

    public decimal? TimeSavedHoursPerWeek { get; init; }
}

public sealed class ContributionSummaryContext
{
    public string Title { get; init; } = null!;

    /// <summary>Populated for Blog, left null for Newsletter — Newsletter's rule is Title/KeyTakeaway only.</summary>
    public string? Description { get; init; }

    public string? KeyTakeaway { get; init; }
}
