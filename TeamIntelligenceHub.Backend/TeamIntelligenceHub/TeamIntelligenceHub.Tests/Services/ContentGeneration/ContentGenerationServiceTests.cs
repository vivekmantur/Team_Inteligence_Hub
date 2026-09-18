using FluentAssertions;
using Moq;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Application.Services;
using TeamIntelligenceHub.Application.Services.ContentGeneration;
using TeamIntelligenceHub.Domain.Entities;
using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Tests.Services.ContentGeneration;

public class ContentGenerationServiceTests
{
    private readonly Mock<IInitiativeRepository> _initiativeRepository = new();
    private readonly Mock<IContributionRepository> _contributionRepository = new();
    private readonly Mock<IChatCompletionClient> _chatClient = new();

    private readonly ContentGenerationService _sut;

    public ContentGenerationServiceTests()
    {
        // A working default so every test that isn't specifically about the chat
        // completion result doesn't have to set one up.
        _chatClient
            .Setup(c => c.CompleteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Generated content");

        _sut = new ContentGenerationService(
            _initiativeRepository.Object, _contributionRepository.Object, _chatClient.Object);
    }

    private static Initiative CreateInitiative(int id = 1) => new() { Id = id, Name = "Test Initiative" };

    private static ContentGenerationRequestDto CreateRequest(ContentFormat format) => new()
    {
        Format = format,
        Tone = ContentTone.Confident,
        Audience = ContentAudience.Leadership,
        Length = ContentLength.Medium
    };

    private void SetupInitiative(int id, Initiative? initiative)
    {
        _initiativeRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(initiative);
    }

    private void SetupContributions(int initiativeId, List<Contribution>? contributions = null)
    {
        _contributionRepository
            .Setup(r => r.GetByInitiativeIdAsync(initiativeId))
            .ReturnsAsync(contributions ?? []);
    }

    [Fact]
    public async Task GenerateAsync_WhenInitiativeDoesNotExist_ThrowsNotFoundException()
    {
        SetupInitiative(999, null);

        var act = () => _sut.GenerateAsync(999, CreateRequest(ContentFormat.ExecutiveSummary));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GenerateAsync_DoesNotLoadContributions_WhenInitiativeDoesNotExist()
    {
        SetupInitiative(999, null);

        var act = () => _sut.GenerateAsync(999, CreateRequest(ContentFormat.ExecutiveSummary));

        await act.Should().ThrowAsync<NotFoundException>();
        _contributionRepository.Verify(
            r => r.GetByInitiativeIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GenerateAsync_DoesNotCallTheChatClient_WhenInitiativeDoesNotExist()
    {
        SetupInitiative(999, null);

        var act = () => _sut.GenerateAsync(999, CreateRequest(ContentFormat.ExecutiveSummary));

        await act.Should().ThrowAsync<NotFoundException>();
        _chatClient.Verify(
            c => c.CompleteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(ContentFormat.LinkedInPost)]
    [InlineData(ContentFormat.VivaEngagePost)]
    [InlineData(ContentFormat.Newsletter)]
    [InlineData(ContentFormat.ExecutiveSummary)]
    [InlineData(ContentFormat.QbrSlide)]
    [InlineData(ContentFormat.Blog)]
    [InlineData(ContentFormat.CaseStudy)]
    public async Task GenerateAsync_ReachesTheProviderSuccessfully_ForEveryFormat(
        ContentFormat format)
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);

        var result = await _sut.GenerateAsync(1, CreateRequest(format));

        result.Format.Should().Be(format);
        result.InitiativeId.Should().Be(1);
        result.Content.Should().NotBeNullOrWhiteSpace();
        _chatClient.Verify(
            c => c.CompleteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(ContentFormat.LinkedInPost)]
    [InlineData(ContentFormat.Blog)]
    [InlineData(ContentFormat.CaseStudy)]
    public async Task GenerateAsync_NeverUsesDocumentRetrieval_ForAnyFormatInThisSlice(
        ContentFormat format)
    {
        // Blog and CaseStudy are structured-only in this slice — Initiative-scoped RAG
        // is a separate follow-up feature, not started here.
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);

        var result = await _sut.GenerateAsync(1, CreateRequest(format));

        result.UsedDocumentRetrieval.Should().BeFalse();
        result.SourceCount.Should().Be(0);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsTheChatClientsContentVerbatim()
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);
        _chatClient
            .Setup(c => c.CompleteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("This is the model's generated text.");

        var result = await _sut.GenerateAsync(1, CreateRequest(ContentFormat.LinkedInPost));

        result.Content.Should().Be("This is the model's generated text.");
    }

    [Fact]
    public async Task GenerateAsync_LeavesGenerationIdNull()
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);

        var result = await _sut.GenerateAsync(1, CreateRequest(ContentFormat.LinkedInPost));

        result.GenerationId.Should().BeNull();
    }

    [Fact]
    public async Task GenerateAsync_CallsTheChatClientWithExactlyThePromptBuilderOutput()
    {
        var initiative = CreateInitiative();
        var contributions = new List<Contribution>
        {
            new()
            {
                Id = 1,
                InitiativeId = 1,
                Title = "A contribution",
                Description = "Description",
                KeyTakeaway = "A takeaway",
                Types = [ContributionType.ProgressUpdate],
                Metric = new ContributionMetric { MetricName = "Adoption", CurrentValue = 43 }
            }
        };
        SetupInitiative(1, initiative);
        SetupContributions(1, contributions);
        var request = CreateRequest(ContentFormat.LinkedInPost);

        await _sut.GenerateAsync(1, request);

        var expectedContext = ContentGenerationContextBuilder.Build(
            ContentFormat.LinkedInPost, initiative, contributions);
        var expectedPrompt = ContentPromptBuilder.Build(
            ContentFormat.LinkedInPost,
            expectedContext,
            request.Tone!.Value,
            request.Audience!.Value,
            request.Length!.Value);

        _chatClient.Verify(
            c => c.CompleteAsync(
                expectedPrompt.SystemPrompt, expectedPrompt.UserPrompt, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_PassesRequestInstructionsThroughToThePrompt()
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);
        var request = CreateRequest(ContentFormat.LinkedInPost);
        request.Instructions = "  Focus on APAC.  ";

        await _sut.GenerateAsync(1, request);

        _chatClient.Verify(
            c => c.CompleteAsync(
                It.IsAny<string>(),
                It.Is<string>(userPrompt => userPrompt.Contains("User instructions: Focus on APAC.")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_WithNoInstructions_OmitsTheSectionFromThePrompt()
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);
        var request = CreateRequest(ContentFormat.LinkedInPost);
        request.Instructions = null;

        await _sut.GenerateAsync(1, request);

        _chatClient.Verify(
            c => c.CompleteAsync(
                It.IsAny<string>(),
                It.Is<string>(userPrompt => !userPrompt.Contains("User instructions")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GenerateAsync_WhenModelOutputIsEmptyOrWhitespace_ThrowsCopilotExceptionRatherThanReturningEmptyContent(
        string? emptyContent)
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);
        _chatClient
            .Setup(c => c.CompleteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyContent!);

        var act = () => _sut.GenerateAsync(1, CreateRequest(ContentFormat.LinkedInPost));

        await act.Should().ThrowAsync<CopilotException>();
    }

    [Fact]
    public async Task GenerateAsync_WhenTheChatClientThrows_PropagatesCopilotException()
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);
        _chatClient
            .Setup(c => c.CompleteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CopilotException("Provider unavailable."));

        var act = () => _sut.GenerateAsync(1, CreateRequest(ContentFormat.LinkedInPost));

        await act.Should().ThrowAsync<CopilotException>();
    }

    [Fact]
    public async Task GenerateAsync_WithNoContributions_StillReturnsAValidResponse()
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1, []);

        var result = await _sut.GenerateAsync(1, CreateRequest(ContentFormat.LinkedInPost));

        result.Should().NotBeNull();
        result.Content.Should().NotBeNullOrWhiteSpace();
        result.UsedDocumentRetrieval.Should().BeFalse();
        result.SourceCount.Should().Be(0);
    }

    [Fact]
    public async Task GenerateAsync_LoadsContributions_ScopedToTheGivenInitiative()
    {
        SetupInitiative(7, CreateInitiative(7));
        SetupContributions(7);

        await _sut.GenerateAsync(7, CreateRequest(ContentFormat.Newsletter));

        _contributionRepository.Verify(r => r.GetByInitiativeIdAsync(7), Times.Once);
    }

    [Fact]
    public void ContentGenerationService_HasNoDependencyOnVectorSearchOrEmbeddingClients()
    {
        // The real guarantee that Blog/CaseStudy cannot invoke RAG or vector-search: the
        // service's constructor does not accept either interface, for any format, so
        // there is nothing to call even if a future change tried to.
        var constructorParameterTypes = typeof(ContentGenerationService)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType.Name);

        constructorParameterTypes.Should().NotContain("IVectorSearchClient");
        constructorParameterTypes.Should().NotContain("IEmbeddingClient");
    }

    [Fact]
    public void ContentGenerationService_HasNoLoggerDependency()
    {
        // No ILogger is injected here at all, so the service layer structurally cannot
        // log a prompt, database value, or generated content — there is nothing to log
        // with. Only the controller logs, and only a generic, non-sensitive message (see
        // ContentGenerationControllerTests).
        var constructorParameterTypes = typeof(ContentGenerationService)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType.Name);

        constructorParameterTypes.Should().NotContain(name => name.Contains("Logger"));
    }

    // -----------------------------------------------------------------------
    // Session Context Slice 3 — previous turns
    // -----------------------------------------------------------------------

    private string _capturedUserPrompt = null!;

    private void CaptureUserPrompt()
    {
        _chatClient
            .Setup(c => c.CompleteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, userPrompt, _) => _capturedUserPrompt = userPrompt)
            .ReturnsAsync("Generated content");
    }

    [Fact]
    public async Task GenerateAsync_WithNullPreviousTurns_MapsToNoTurns()
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);
        CaptureUserPrompt();
        var request = CreateRequest(ContentFormat.LinkedInPost);
        request.PreviousTurns = null;

        await _sut.GenerateAsync(1, request);

        _capturedUserPrompt.Should().NotContain("Previous turn");
    }

    [Fact]
    public async Task GenerateAsync_WithEmptyPreviousTurns_MapsToNoTurns()
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);
        CaptureUserPrompt();
        var request = CreateRequest(ContentFormat.LinkedInPost);
        request.PreviousTurns = [];

        await _sut.GenerateAsync(1, request);

        _capturedUserPrompt.Should().NotContain("Previous turn");
    }

    [Fact]
    public async Task GenerateAsync_WithOnePreviousTurn_ReachesThePromptBuilder()
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);
        CaptureUserPrompt();
        var request = CreateRequest(ContentFormat.LinkedInPost);
        request.PreviousTurns =
        [
            new ContentGenerationTurnDto { Instruction = "Make it punchier.", Output = "Prior output text." }
        ];

        await _sut.GenerateAsync(1, request);

        _capturedUserPrompt.Should().Contain("Previous turn 1 instruction:");
        _capturedUserPrompt.Should().Contain("Make it punchier.");
        _capturedUserPrompt.Should().Contain("Previous turn 1 output:");
        _capturedUserPrompt.Should().Contain("Prior output text.");
    }

    [Fact]
    public async Task GenerateAsync_WithMultiplePreviousTurns_PreservesOrder()
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);
        CaptureUserPrompt();
        var request = CreateRequest(ContentFormat.LinkedInPost);
        request.PreviousTurns =
        [
            new ContentGenerationTurnDto { Instruction = "FIRST_MARKER", Output = "First output." },
            new ContentGenerationTurnDto { Instruction = "SECOND_MARKER", Output = "Second output." },
            new ContentGenerationTurnDto { Instruction = "THIRD_MARKER", Output = "Third output." },
        ];

        await _sut.GenerateAsync(1, request);

        var firstIndex = _capturedUserPrompt.IndexOf("FIRST_MARKER", StringComparison.Ordinal);
        var secondIndex = _capturedUserPrompt.IndexOf("SECOND_MARKER", StringComparison.Ordinal);
        var thirdIndex = _capturedUserPrompt.IndexOf("THIRD_MARKER", StringComparison.Ordinal);

        firstIndex.Should().BeGreaterThan(-1);
        secondIndex.Should().BeGreaterThan(firstIndex);
        thirdIndex.Should().BeGreaterThan(secondIndex);
    }

    [Fact]
    public async Task GenerateAsync_PreviousInstructionAndOutputValues_ReachTheUserPrompt()
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);
        CaptureUserPrompt();
        var request = CreateRequest(ContentFormat.LinkedInPost);
        request.PreviousTurns =
        [
            new ContentGenerationTurnDto { Instruction = "UNIQUE_INSTRUCTION_31ab", Output = "UNIQUE_OUTPUT_88cd" }
        ];

        await _sut.GenerateAsync(1, request);

        _capturedUserPrompt.Should().Contain("UNIQUE_INSTRUCTION_31ab");
        _capturedUserPrompt.Should().Contain("UNIQUE_OUTPUT_88cd");
    }

    [Fact]
    public async Task GenerateAsync_PreviousTurns_DoNotReachTheSystemPrompt()
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);
        var request = CreateRequest(ContentFormat.LinkedInPost);
        request.PreviousTurns =
        [
            new ContentGenerationTurnDto { Instruction = "UNIQUE_INSTRUCTION_71fe", Output = "UNIQUE_OUTPUT_92ac" }
        ];

        await _sut.GenerateAsync(1, request);

        _chatClient.Verify(
            c => c.CompleteAsync(
                It.Is<string>(systemPrompt =>
                    !systemPrompt.Contains("UNIQUE_INSTRUCTION_71fe")
                    && !systemPrompt.Contains("UNIQUE_OUTPUT_92ac")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_CurrentInstructions_RemainAfterPreviousTurns()
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);
        CaptureUserPrompt();
        var request = CreateRequest(ContentFormat.LinkedInPost);
        request.Instructions = "Current instruction text.";
        request.PreviousTurns =
        [
            new ContentGenerationTurnDto { Instruction = "Earlier instruction.", Output = "Earlier output." }
        ];

        await _sut.GenerateAsync(1, request);

        var previousTurnIndex = _capturedUserPrompt.IndexOf("Previous turn 1 instruction:", StringComparison.Ordinal);
        var currentInstructionIndex = _capturedUserPrompt.IndexOf("User instructions:", StringComparison.Ordinal);

        previousTurnIndex.Should().BeGreaterThan(-1);
        currentInstructionIndex.Should().BeGreaterThan(previousTurnIndex);
    }

    [Theory]
    [InlineData(ContentFormat.LinkedInPost)]
    [InlineData(ContentFormat.VivaEngagePost)]
    [InlineData(ContentFormat.Newsletter)]
    [InlineData(ContentFormat.ExecutiveSummary)]
    [InlineData(ContentFormat.QbrSlide)]
    [InlineData(ContentFormat.Blog)]
    [InlineData(ContentFormat.CaseStudy)]
    public async Task GenerateAsync_ExistingFormatBehavior_RemainsValidWithHistory(ContentFormat format)
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);
        var request = CreateRequest(format);
        request.PreviousTurns =
        [
            new ContentGenerationTurnDto { Instruction = "Earlier instruction.", Output = "Earlier output." }
        ];

        var result = await _sut.GenerateAsync(1, request);

        result.Format.Should().Be(format);
        result.InitiativeId.Should().Be(1);
        result.Content.Should().NotBeNullOrWhiteSpace();
        _chatClient.Verify(
            c => c.CompleteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_WithPreviousTurns_InitiativeNotFound_StillPreventsChatClientCalls()
    {
        SetupInitiative(999, null);
        var request = CreateRequest(ContentFormat.ExecutiveSummary);
        request.PreviousTurns =
        [
            new ContentGenerationTurnDto { Instruction = "Earlier instruction.", Output = "Earlier output." }
        ];

        var act = () => _sut.GenerateAsync(999, request);

        await act.Should().ThrowAsync<NotFoundException>();
        _chatClient.Verify(
            c => c.CompleteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GenerateAsync_WithPreviousTurns_ProviderFailure_StillPropagatesAsCopilotException()
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);
        _chatClient
            .Setup(c => c.CompleteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CopilotException("Provider unavailable."));
        var request = CreateRequest(ContentFormat.LinkedInPost);
        request.PreviousTurns =
        [
            new ContentGenerationTurnDto { Instruction = "Earlier instruction.", Output = "Earlier output." }
        ];

        var act = () => _sut.GenerateAsync(1, request);

        await act.Should().ThrowAsync<CopilotException>();
    }

    [Fact]
    public async Task GenerateAsync_WithPreviousTurns_EmptyModelOutput_StillFails()
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);
        _chatClient
            .Setup(c => c.CompleteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);
        var request = CreateRequest(ContentFormat.LinkedInPost);
        request.PreviousTurns =
        [
            new ContentGenerationTurnDto { Instruction = "Earlier instruction.", Output = "Earlier output." }
        ];

        var act = () => _sut.GenerateAsync(1, request);

        await act.Should().ThrowAsync<CopilotException>();
    }

    [Fact]
    public async Task GenerateAsync_WithPreviousTurns_UsedDocumentRetrievalRemainsFalse()
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);
        var request = CreateRequest(ContentFormat.LinkedInPost);
        request.PreviousTurns =
        [
            new ContentGenerationTurnDto { Instruction = "Earlier instruction.", Output = "Earlier output." }
        ];

        var result = await _sut.GenerateAsync(1, request);

        result.UsedDocumentRetrieval.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateAsync_WithPreviousTurns_SourceCountRemainsZero()
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);
        var request = CreateRequest(ContentFormat.LinkedInPost);
        request.PreviousTurns =
        [
            new ContentGenerationTurnDto { Instruction = "Earlier instruction.", Output = "Earlier output." }
        ];

        var result = await _sut.GenerateAsync(1, request);

        result.SourceCount.Should().Be(0);
    }

    [Fact]
    public async Task GenerateAsync_WithPreviousTurns_GenerationIdRemainsNull()
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);
        var request = CreateRequest(ContentFormat.LinkedInPost);
        request.PreviousTurns =
        [
            new ContentGenerationTurnDto { Instruction = "Earlier instruction.", Output = "Earlier output." }
        ];

        var result = await _sut.GenerateAsync(1, request);

        result.GenerationId.Should().BeNull();
    }

    [Fact]
    public async Task GenerateAsync_RetainsNoServerSideHistory_BetweenTwoIndependentCalls()
    {
        SetupInitiative(1, CreateInitiative());
        SetupContributions(1);
        var capturedPrompts = new List<string>();
        _chatClient
            .Setup(c => c.CompleteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, userPrompt, _) => capturedPrompts.Add(userPrompt))
            .ReturnsAsync("Generated content");

        var firstRequest = CreateRequest(ContentFormat.LinkedInPost);
        firstRequest.Instructions = "FIRST_CALL_MARKER";
        await _sut.GenerateAsync(1, firstRequest);

        // A second, independent call carries no PreviousTurns of its own — nothing from
        // the first call's instruction is appended or persisted anywhere in the service.
        var secondRequest = CreateRequest(ContentFormat.LinkedInPost);
        secondRequest.Instructions = "SECOND_CALL_MARKER";
        await _sut.GenerateAsync(1, secondRequest);

        capturedPrompts.Should().HaveCount(2);
        capturedPrompts[0].Should().Contain("FIRST_CALL_MARKER");
        capturedPrompts[1].Should().Contain("SECOND_CALL_MARKER");
        capturedPrompts[1].Should().NotContain("FIRST_CALL_MARKER");
        capturedPrompts[1].Should().NotContain("Previous turn");
    }
}
