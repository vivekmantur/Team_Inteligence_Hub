using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Application.Services.ContentGeneration;

/// <summary>
/// Everything a prompt for one Content Studio generation is allowed to see, already
/// translated out of EF entities. Every format populates only the fields its own context
/// rule names — the rest stay at their empty default, which is what makes "this format
/// used only its specified data" a fact about the object rather than a convention callers
/// have to remember. It deliberately has no Contributor, Link, or Attachment properties.
/// </summary>
public sealed class ContentGenerationContext
{
    /// <summary>Business metrics with their before and after values.</summary>
    public IReadOnlyList<MetricContext> Metrics { get; init; } = [];

    /// <summary>Just the quotable line and who said it — LinkedInPost's "CustomerStory.Quote".</summary>
    public IReadOnlyList<CustomerQuoteContext> CustomerQuotes { get; init; } = [];

    /// <summary>The full customer-story backbone — Blog and CaseStudy.</summary>
    public IReadOnlyList<CustomerStoryContext> CustomerStories { get; init; } = [];

    /// <summary>Risks with their severity and business impact.</summary>
    public IReadOnlyList<RiskContext> Risks { get; init; } = [];

    /// <summary>AI best practices with their tool, use case, and time saved.</summary>
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

    /// <summary>The Initiative's success measures, for QbrSlide.</summary>
    public string? InitiativeSuccessMeasures { get; init; }

    /// <summary>The Initiative's key objective, for QbrSlide.</summary>
    public string? InitiativeKeyObjective { get; init; }
}

/// <summary>One business metric with its before and after values.</summary>
public sealed class MetricContext
{
    /// <summary>The metric's name.</summary>
    public string MetricName { get; init; } = null!;

    /// <summary>The unit the values are measured in.</summary>
    public string? Unit { get; init; }

    /// <summary>The value before the change.</summary>
    public decimal? PreviousValue { get; init; }

    /// <summary>The value after the change.</summary>
    public decimal? CurrentValue { get; init; }
}

/// <summary>A customer quote and the customer it came from.</summary>
public sealed class CustomerQuoteContext
{
    /// <summary>The customer the quote came from.</summary>
    public string CustomerName { get; init; } = null!;

    /// <summary>The quotable line.</summary>
    public string? Quote { get; init; }
}

/// <summary>The fields of one customer story.</summary>
public sealed class CustomerStoryContext
{
    /// <summary>The customer the story is about.</summary>
    public string CustomerName { get; init; } = null!;

    /// <summary>A short summary of the story.</summary>
    public string? Summary { get; init; }

    /// <summary>What the work achieved for the customer.</summary>
    public string? Outcome { get; init; }

    /// <summary>A quote from the customer.</summary>
    public string? Quote { get; init; }

    /// <summary>The business value the customer received.</summary>
    public string? BusinessValue { get; init; }
}

/// <summary>One risk with its severity and business impact.</summary>
public sealed class RiskContext
{
    /// <summary>What the risk is.</summary>
    public string Description { get; init; } = null!;

    /// <summary>How severe the risk is.</summary>
    public RiskSeverity Severity { get; init; }

    /// <summary>What the risk would cost the business.</summary>
    public string? BusinessImpact { get; init; }
}

/// <summary>One AI best practice: the tool, its use case, and the time it saves.</summary>
public sealed class AiPracticeContext
{
    /// <summary>The AI tool used.</summary>
    public string Tool { get; init; } = null!;

    /// <summary>What the tool is used for.</summary>
    public string? UseCase { get; init; }

    /// <summary>Hours saved per week by the practice.</summary>
    public decimal? TimeSavedHoursPerWeek { get; init; }
}

/// <summary>A contribution's base narrative fields.</summary>
public sealed class ContributionSummaryContext
{
    /// <summary>The contribution's title.</summary>
    public string Title { get; init; } = null!;

    /// <summary>Populated for Blog, left null for Newsletter — Newsletter's rule is Title/KeyTakeaway only.</summary>
    public string? Description { get; init; }

    /// <summary>The contribution's key takeaway.</summary>
    public string? KeyTakeaway { get; init; }
}
