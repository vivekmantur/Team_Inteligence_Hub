using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TeamIntelligenceHub.API.Controllers;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Services;
using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Tests.Controllers;

/// <summary>
/// Constructs ContentGenerationController directly and mocks IContentGenerationService,
/// the same lightweight approach every *ServiceTests.cs class in this project already
/// uses for its own dependencies — no WebApplicationFactory or HTTP pipeline involved, so
/// these tests exercise the controller's own exception-to-status-code mapping, not
/// ASP.NET's model-binding pipeline (see ContentGenerationRequestDtoTests for that).
/// </summary>
public class ContentGenerationControllerTests
{
    private readonly Mock<IContentGenerationService> _contentGenerationService = new();
    private readonly Mock<ILogger<ContentGenerationController>> _logger = new();
    private readonly ContentGenerationController _sut;

    public ContentGenerationControllerTests()
    {
        _sut = new ContentGenerationController(
            _contentGenerationService.Object, _logger.Object);
    }

    private static ContentGenerationRequestDto CreateRequest()
    {
        return new ContentGenerationRequestDto
        {
            Format = ContentFormat.ExecutiveSummary,
            Tone = ContentTone.Confident,
            Audience = ContentAudience.Leadership,
            Length = ContentLength.Medium
        };
    }

    [Fact]
    public async Task Generate_WithValidRequest_CallsServiceWithInitiativeIdAndRequest()
    {
        var request = CreateRequest();
        var response = new ContentGenerationResponseDto
        {
            Content = "Generated content",
            Format = ContentFormat.ExecutiveSummary,
            InitiativeId = 42,
            UsedDocumentRetrieval = false,
            SourceCount = 0
        };

        _contentGenerationService
            .Setup(s => s.GenerateAsync(42, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _sut.Generate(42, request, CancellationToken.None);

        // Confirms the controller wires the route id and the bound body straight through
        // to the service, with no transformation and no extra data attached.
        _contentGenerationService.Verify(
            s => s.GenerateAsync(42, request, It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task Generate_WhenInitiativeDoesNotExist_Returns404()
    {
        var request = CreateRequest();

        _contentGenerationService
            .Setup(s => s.GenerateAsync(999, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Initiative 999 does not exist."));

        var result = await _sut.Generate(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Generate_WhenServiceRejectsTheRequest_Returns400()
    {
        var request = CreateRequest();

        _contentGenerationService
            .Setup(s => s.GenerateAsync(42, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("This format is not yet available."));

        var result = await _sut.Generate(42, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Generate_WhenChatCompletionFails_Returns502WithASanitizedMessage()
    {
        var request = CreateRequest();

        _contentGenerationService
            .Setup(s => s.GenerateAsync(42, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CopilotException(
                "Could not generate an answer: 429. Rate limit exceeded for deployment X."));

        var result = await _sut.Generate(42, request, CancellationToken.None);

        var problem = result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(StatusCodes.Status502BadGateway);

        var body = problem.Value.Should().BeOfType<ProblemDetails>().Subject;
        body.Detail.Should().Be("Content generation is temporarily unavailable. Please try again.");

        // The provider's own message never reaches the client — only the fixed,
        // generic sentence above does.
        body.Detail.Should().NotContain("429");
        body.Detail.Should().NotContain("Rate limit");
    }

    [Fact]
    public async Task Generate_WhenChatCompletionFails_NeverLogsTheProviderExceptionMessage()
    {
        const string providerDetail = "SENSITIVE_PROVIDER_DIAGNOSTIC_7f3a1";
        var request = CreateRequest();

        _contentGenerationService
            .Setup(s => s.GenerateAsync(42, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CopilotException($"Could not generate an answer: {providerDetail}"));

        await _sut.Generate(42, request, CancellationToken.None);

        // Every argument passed to every ILogger call, flattened to text — none of it
        // may contain the provider's own diagnostic message. The controller's catch
        // block deliberately discards the caught exception rather than logging it.
        var loggedText = string.Join(
            " ",
            _logger.Invocations.SelectMany(i => i.Arguments).Select(a => a?.ToString() ?? ""));

        loggedText.Should().NotContain(providerDetail);
    }
}
