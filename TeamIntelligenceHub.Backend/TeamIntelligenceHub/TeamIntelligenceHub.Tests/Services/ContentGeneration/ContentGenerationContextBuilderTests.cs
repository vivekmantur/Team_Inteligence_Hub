using FluentAssertions;
using TeamIntelligenceHub.Application.Services.ContentGeneration;
using TeamIntelligenceHub.Domain.Entities;
using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Tests.Services.ContentGeneration;

public class ContentGenerationContextBuilderTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Initiative CreateInitiative(
        string? expectedOutcome = null,
        string? successMeasures = null,
        string? keyObjective = null)
    {
        return new Initiative
        {
            Id = 1,
            Name = "Test Initiative",
            ExpectedOutcome = expectedOutcome,
            SuccessMeasures = successMeasures,
            KeyObjective = keyObjective
        };
    }

    private static Contribution CreateContribution(
        int id,
        List<ContributionType>? types = null,
        string? keyTakeaway = null,
        ContributionMetric? metric = null,
        ContributionRisk? risk = null,
        ContributionAiPractice? aiPractice = null,
        ContributionCustomerStory? customerStory = null)
    {
        return new Contribution
        {
            Id = id,
            InitiativeId = 1,
            Title = $"Contribution {id}",
            Description = $"Description {id}",
            KeyTakeaway = keyTakeaway,
            Types = types ?? [ContributionType.ProgressUpdate],
            Metric = metric,
            Risk = risk,
            AiPractice = aiPractice,
            CustomerStory = customerStory
        };
    }

    private static ContributionMetric CreateMetric(string name = "Adoption") =>
        new() { MetricName = name, Unit = "%", PreviousValue = 10, CurrentValue = 43 };

    private static ContributionRisk CreateRisk(string description = "A risk") =>
        new() { Description = description, Severity = RiskSeverity.High };

    private static ContributionAiPractice CreateAiPractice(string tool = "Copilot") =>
        new() { Tool = tool, TimeSavedHoursPerWeek = 5 };

    private static ContributionCustomerStory CreateCustomerStory(string customerName = "Contoso") =>
        new()
        {
            CustomerName = customerName,
            Summary = "Summary",
            Outcome = "Outcome",
            Quote = "Great tool.",
            BusinessValue = "Saved money"
        };

    // -----------------------------------------------------------------------
    // Each format receives only its specified context
    // -----------------------------------------------------------------------

    [Fact]
    public void LinkedInPost_UsesMetricsQuotesAndKeyTakeaways_NothingElse()
    {
        var contributions = new List<Contribution>
        {
            CreateContribution(1, metric: CreateMetric(), keyTakeaway: "A quotable line"),
            CreateContribution(2, customerStory: CreateCustomerStory("Contoso")),
            CreateContribution(3, risk: CreateRisk()),
            CreateContribution(4, aiPractice: CreateAiPractice())
        };

        var context = ContentGenerationContextBuilder.Build(
            ContentFormat.LinkedInPost, CreateInitiative(), contributions);

        context.Metrics.Should().ContainSingle();
        context.CustomerQuotes.Should().ContainSingle(q => q.CustomerName == "Contoso");
        context.KeyTakeaways.Should().ContainSingle().Which.Should().Be("A quotable line");

        // Not part of LinkedInPost's rule.
        context.CustomerStories.Should().BeEmpty();
        context.Risks.Should().BeEmpty();
        context.AiPractices.Should().BeEmpty();
        context.ContributionSummaries.Should().BeEmpty();
        context.InitiativeExpectedOutcome.Should().BeNull();
    }

    [Fact]
    public void VivaEngagePost_EmphasizesAiPractice_AndCarriesMetricsAndKeyTakeawaysAsWins()
    {
        var contributions = new List<Contribution>
        {
            CreateContribution(1, aiPractice: CreateAiPractice("Copilot Studio")),
            CreateContribution(2, metric: CreateMetric(), keyTakeaway: "Team shipped fast"),
            CreateContribution(3, risk: CreateRisk())
        };

        var context = ContentGenerationContextBuilder.Build(
            ContentFormat.VivaEngagePost, CreateInitiative(), contributions);

        context.AiPractices.Should().ContainSingle(a => a.Tool == "Copilot Studio");
        context.Metrics.Should().ContainSingle();
        context.KeyTakeaways.Should().ContainSingle();

        // Not part of VivaEngagePost's rule.
        context.Risks.Should().BeEmpty();
        context.CustomerStories.Should().BeEmpty();
        context.CustomerQuotes.Should().BeEmpty();
        context.ContributionSummaries.Should().BeEmpty();
    }

    [Fact]
    public void ExecutiveSummary_UsesMetricsRisksAndKeyTakeaways_NothingElse()
    {
        var contributions = new List<Contribution>
        {
            CreateContribution(1, metric: CreateMetric(), keyTakeaway: "Headline"),
            CreateContribution(2, risk: CreateRisk("Adoption stalling")),
            CreateContribution(3, aiPractice: CreateAiPractice())
        };

        var context = ContentGenerationContextBuilder.Build(
            ContentFormat.ExecutiveSummary, CreateInitiative(), contributions);

        context.Metrics.Should().ContainSingle();
        context.Risks.Should().ContainSingle(r => r.Description == "Adoption stalling");
        context.KeyTakeaways.Should().ContainSingle();

        context.AiPractices.Should().BeEmpty();
        context.CustomerStories.Should().BeEmpty();
        context.ContributionSummaries.Should().BeEmpty();
    }

    [Fact]
    public void CaseStudy_UsesCustomerStoryFieldsOnly()
    {
        var contributions = new List<Contribution>
        {
            CreateContribution(1, customerStory: CreateCustomerStory("Fabrikam")),
            CreateContribution(2, metric: CreateMetric()),
            CreateContribution(3, risk: CreateRisk())
        };

        var context = ContentGenerationContextBuilder.Build(
            ContentFormat.CaseStudy, CreateInitiative(), contributions);

        context.CustomerStories.Should().ContainSingle(s =>
            s.CustomerName == "Fabrikam"
            && s.Summary == "Summary"
            && s.Outcome == "Outcome"
            && s.Quote == "Great tool."
            && s.BusinessValue == "Saved money");

        // CaseStudy's rule names only the CustomerStory fields — not Metric, unlike Blog.
        context.Metrics.Should().BeEmpty();
        context.Risks.Should().BeEmpty();
        context.KeyTakeaways.Should().BeEmpty();
        context.ContributionSummaries.Should().BeEmpty();
    }

    [Fact]
    public void Blog_UsesStructuredDataOnly_MetricsCustomerStoriesAndDeliverableNarrative()
    {
        var contributions = new List<Contribution>
        {
            CreateContribution(1, types: [ContributionType.BusinessMetric], metric: CreateMetric()),
            CreateContribution(
                2, types: [ContributionType.CustomerStory], customerStory: CreateCustomerStory()),
            CreateContribution(
                3, types: [ContributionType.Deliverable], keyTakeaway: "Shipped the thing"),
            CreateContribution(4, types: [ContributionType.Risk]) // not Deliverable/ProgressUpdate
        };

        var context = ContentGenerationContextBuilder.Build(
            ContentFormat.Blog, CreateInitiative(), contributions);

        context.Metrics.Should().ContainSingle();
        context.CustomerStories.Should().ContainSingle();
        context.ContributionSummaries.Should().ContainSingle(s => s.Title == "Contribution 3");

        // Blog's narrative includes Description, unlike Newsletter's.
        var summary = context.ContributionSummaries.Single(s => s.Title == "Contribution 3");
        summary.Description.Should().Be("Description 3");
        summary.KeyTakeaway.Should().Be("Shipped the thing");

        // No document retrieval — structured only, per this slice's scope.
        context.Risks.Should().BeEmpty();
        context.AiPractices.Should().BeEmpty();
    }

    [Fact]
    public void QbrSlide_UsesMetricsAndInitiativeRoadmapFields()
    {
        var initiative = CreateInitiative(
            expectedOutcome: "Faster onboarding",
            successMeasures: "Time to productivity",
            keyObjective: "Scale Role Hub");
        var contributions = new List<Contribution> { CreateContribution(1, metric: CreateMetric()) };

        var context = ContentGenerationContextBuilder.Build(
            ContentFormat.QbrSlide, initiative, contributions);

        context.Metrics.Should().ContainSingle();
        context.InitiativeExpectedOutcome.Should().Be("Faster onboarding");
        context.InitiativeSuccessMeasures.Should().Be("Time to productivity");
        context.InitiativeKeyObjective.Should().Be("Scale Role Hub");

        // QbrSlide's rule does not name KeyTakeaway.
        context.KeyTakeaways.Should().BeEmpty();
        context.ContributionSummaries.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // In-memory Types filtering (Newsletter, Blog)
    // -----------------------------------------------------------------------

    [Fact]
    public void Newsletter_FiltersDeliverableAndProgressUpdate_InMemory_AmongOtherTypes()
    {
        var contributions = new List<Contribution>
        {
            CreateContribution(1, types: [ContributionType.Deliverable]),
            CreateContribution(2, types: [ContributionType.ProgressUpdate]),
            CreateContribution(3, types: [ContributionType.BusinessMetric]),
            CreateContribution(4, types: [ContributionType.CustomerStory]),
            CreateContribution(5, types: [ContributionType.Risk]),
            CreateContribution(6, types: [ContributionType.AiBestPractice])
        };

        var context = ContentGenerationContextBuilder.Build(
            ContentFormat.Newsletter, CreateInitiative(), contributions);

        context.ContributionSummaries.Select(s => s.Title).Should().BeEquivalentTo(
            "Contribution 1", "Contribution 2", "Contribution 3", "Contribution 4");

        // Newsletter's rule is Title/KeyTakeaway only — no Description.
        context.ContributionSummaries.Should().OnlyContain(s => s.Description == null);
    }

    [Fact]
    public void DuplicateTypeValues_DoNotDuplicateAContribution()
    {
        // One Contribution matches two of Newsletter's requested types at once.
        var contributions = new List<Contribution>
        {
            CreateContribution(
                1, types: [ContributionType.Deliverable, ContributionType.ProgressUpdate])
        };

        var context = ContentGenerationContextBuilder.Build(
            ContentFormat.Newsletter, CreateInitiative(), contributions);

        context.ContributionSummaries.Should().ContainSingle();
    }

    // -----------------------------------------------------------------------
    // Empty input
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(ContentFormat.LinkedInPost)]
    [InlineData(ContentFormat.VivaEngagePost)]
    [InlineData(ContentFormat.Newsletter)]
    [InlineData(ContentFormat.ExecutiveSummary)]
    [InlineData(ContentFormat.QbrSlide)]
    [InlineData(ContentFormat.Blog)]
    [InlineData(ContentFormat.CaseStudy)]
    public void Build_WithNoContributions_ReturnsEmptyContextRatherThanThrowing(ContentFormat format)
    {
        var act = () => ContentGenerationContextBuilder.Build(
            format, CreateInitiative(), []);

        act.Should().NotThrow();

        var context = act();
        context.Metrics.Should().BeEmpty();
        context.CustomerStories.Should().BeEmpty();
        context.CustomerQuotes.Should().BeEmpty();
        context.Risks.Should().BeEmpty();
        context.AiPractices.Should().BeEmpty();
        context.KeyTakeaways.Should().BeEmpty();
        context.ContributionSummaries.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // No Contributor, Link, or Attachment data can ever reach the context —
    // there is no property on ContentGenerationContext to carry any of it.
    // -----------------------------------------------------------------------

    [Fact]
    public void ContentGenerationContext_ExposesNoContributorLinkOrAttachmentProperty()
    {
        var propertyNames = typeof(ContentGenerationContext)
            .GetProperties()
            .Select(p => p.Name);

        propertyNames.Should().NotContain(name =>
            name.Contains("Contributor") || name.Contains("Link") || name.Contains("Attachment"));
    }
}
