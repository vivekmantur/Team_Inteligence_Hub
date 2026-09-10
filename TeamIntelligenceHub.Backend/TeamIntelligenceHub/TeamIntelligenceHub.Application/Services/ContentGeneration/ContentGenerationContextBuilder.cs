using TeamIntelligenceHub.Domain.Entities;
using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Application.Services.ContentGeneration;

/// <summary>
/// Translates an Initiative's already-loaded, already-Initiative-scoped Contribution list
/// into the narrow context each format's generation rule calls for.
/// </summary>
/// <remarks>
/// The only place in this feature that touches Initiative/Contribution EF entities.
/// Everything downstream — prompt construction, the Azure OpenAI call — sees only
/// ContentGenerationContext, never these entities.
///
/// Per-type filtering (Newsletter, Blog) is done in memory over the list the caller
/// already fetched via IContributionRepository.GetByInitiativeIdAsync, deliberately
/// avoiding a Types.Contains(...) LINQ-to-SQL query — Types is a JSON PrimitiveCollection
/// column with no established precedent in this codebase for that translating correctly.
/// </remarks>
public static class ContentGenerationContextBuilder
{
    public static ContentGenerationContext Build(
        ContentFormat format, Initiative initiative, IReadOnlyList<Contribution> contributions)
    {
        return format switch
        {
            ContentFormat.LinkedInPost => BuildLinkedInPost(contributions),
            ContentFormat.VivaEngagePost => BuildVivaEngagePost(contributions),
            ContentFormat.Newsletter => BuildNewsletter(contributions),
            ContentFormat.ExecutiveSummary => BuildExecutiveSummary(contributions),
            ContentFormat.QbrSlide => BuildQbrSlide(initiative, contributions),
            ContentFormat.Blog => BuildBlog(contributions),
            ContentFormat.CaseStudy => BuildCaseStudy(contributions),
            _ => throw new ArgumentOutOfRangeException(
                nameof(format), format, "Unsupported content format.")
        };
    }

    // -----------------------------------------------------------------------
    // Per-format context assembly
    // -----------------------------------------------------------------------

    private static ContentGenerationContext BuildLinkedInPost(
        IReadOnlyList<Contribution> contributions) =>
        new()
        {
            Metrics = GetMetrics(contributions),
            CustomerQuotes = GetCustomerQuotes(contributions),
            KeyTakeaways = GetKeyTakeaways(contributions)
        };

    /// <summary>
    /// "Structured data, especially AiBestPractice and team wins" is not more specific
    /// than that in the spec. Read here as: AiPractice is the type this format actually
    /// singles out, and Metrics/KeyTakeaways stand in for "team wins" — the same
    /// quantifiable-and-quotable signals LinkedInPost uses. Not silently resolved: this
    /// interpretation is called out in the Slice 2 report rather than assumed without
    /// comment.
    /// </summary>
    private static ContentGenerationContext BuildVivaEngagePost(
        IReadOnlyList<Contribution> contributions) =>
        new()
        {
            AiPractices = GetAiPractices(contributions),
            Metrics = GetMetrics(contributions),
            KeyTakeaways = GetKeyTakeaways(contributions)
        };

    private static ContentGenerationContext BuildNewsletter(
        IReadOnlyList<Contribution> contributions) =>
        new()
        {
            ContributionSummaries = GetContributionSummaries(
                contributions,
                includeDescription: false,
                ContributionType.Deliverable,
                ContributionType.ProgressUpdate,
                ContributionType.BusinessMetric,
                ContributionType.CustomerStory)
        };

    private static ContentGenerationContext BuildExecutiveSummary(
        IReadOnlyList<Contribution> contributions) =>
        new()
        {
            Metrics = GetMetrics(contributions),
            Risks = GetRisks(contributions),
            KeyTakeaways = GetKeyTakeaways(contributions)
        };

    private static ContentGenerationContext BuildQbrSlide(
        Initiative initiative, IReadOnlyList<Contribution> contributions) =>
        new()
        {
            Metrics = GetMetrics(contributions),
            InitiativeExpectedOutcome = initiative.ExpectedOutcome,
            InitiativeSuccessMeasures = initiative.SuccessMeasures,
            InitiativeKeyObjective = initiative.KeyObjective
        };

    /// <summary>Structured only — no attachment retrieval. RAG is a separate follow-up feature.</summary>
    private static ContentGenerationContext BuildBlog(
        IReadOnlyList<Contribution> contributions) =>
        new()
        {
            Metrics = GetMetrics(contributions),
            CustomerStories = GetCustomerStories(contributions),
            ContributionSummaries = GetContributionSummaries(
                contributions,
                includeDescription: true,
                ContributionType.Deliverable,
                ContributionType.ProgressUpdate)
        };

    /// <summary>Structured only — no attachment retrieval. RAG is a separate follow-up feature.</summary>
    private static ContentGenerationContext BuildCaseStudy(
        IReadOnlyList<Contribution> contributions) =>
        new()
        {
            CustomerStories = GetCustomerStories(contributions)
        };

    // -----------------------------------------------------------------------
    // Field extraction — keyed off detail-row presence, not Types
    // -----------------------------------------------------------------------

    private static List<MetricContext> GetMetrics(IReadOnlyList<Contribution> contributions) =>
        contributions
            .Where(c => c.Metric is not null)
            .Select(c => new MetricContext
            {
                MetricName = c.Metric!.MetricName,
                Unit = c.Metric.Unit,
                PreviousValue = c.Metric.PreviousValue,
                CurrentValue = c.Metric.CurrentValue
            })
            .ToList();

    private static List<CustomerQuoteContext> GetCustomerQuotes(
        IReadOnlyList<Contribution> contributions) =>
        contributions
            .Where(c => c.CustomerStory is not null)
            .Select(c => new CustomerQuoteContext
            {
                CustomerName = c.CustomerStory!.CustomerName,
                Quote = c.CustomerStory.Quote
            })
            .ToList();

    private static List<CustomerStoryContext> GetCustomerStories(
        IReadOnlyList<Contribution> contributions) =>
        contributions
            .Where(c => c.CustomerStory is not null)
            .Select(c => new CustomerStoryContext
            {
                CustomerName = c.CustomerStory!.CustomerName,
                Summary = c.CustomerStory.Summary,
                Outcome = c.CustomerStory.Outcome,
                Quote = c.CustomerStory.Quote,
                BusinessValue = c.CustomerStory.BusinessValue
            })
            .ToList();

    private static List<RiskContext> GetRisks(IReadOnlyList<Contribution> contributions) =>
        contributions
            .Where(c => c.Risk is not null)
            .Select(c => new RiskContext
            {
                Description = c.Risk!.Description,
                Severity = c.Risk.Severity,
                BusinessImpact = c.Risk.BusinessImpact
            })
            .ToList();

    private static List<AiPracticeContext> GetAiPractices(
        IReadOnlyList<Contribution> contributions) =>
        contributions
            .Where(c => c.AiPractice is not null)
            .Select(c => new AiPracticeContext
            {
                Tool = c.AiPractice!.Tool,
                UseCase = c.AiPractice.UseCase,
                TimeSavedHoursPerWeek = c.AiPractice.TimeSavedHoursPerWeek
            })
            .ToList();

    private static List<string> GetKeyTakeaways(IReadOnlyList<Contribution> contributions) =>
        contributions
            .Where(c => !string.IsNullOrWhiteSpace(c.KeyTakeaway))
            .Select(c => c.KeyTakeaway!)
            .ToList();

    /// <summary>
    /// In-memory Types filter over the already-loaded, already-Initiative-scoped list.
    /// DistinctBy(Id) is what stops a Contribution typed both Deliverable and
    /// ProgressUpdate — matching two of the requested types at once — from being added
    /// twice.
    /// </summary>
    private static List<ContributionSummaryContext> GetContributionSummaries(
        IReadOnlyList<Contribution> contributions,
        bool includeDescription,
        params ContributionType[] types) =>
        contributions
            .Where(c => c.Types.Any(t => types.Contains(t)))
            .DistinctBy(c => c.Id)
            .Select(c => new ContributionSummaryContext
            {
                Title = c.Title,
                Description = includeDescription ? c.Description : null,
                KeyTakeaway = c.KeyTakeaway
            })
            .ToList();
}
