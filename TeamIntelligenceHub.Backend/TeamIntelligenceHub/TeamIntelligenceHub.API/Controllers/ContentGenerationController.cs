using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Services;

namespace TeamIntelligenceHub.API.Controllers;

/// <summary>
/// Generates Content Studio output (LinkedIn Post, Newsletter, Blog, and so on) grounded
/// on one Initiative's own data.
/// </summary>
/// <remarks>
/// Separate from InitiativesController on purpose: this depends on the Azure OpenAI chat
/// client rather than InitiativesController's CRUD concerns, the same reason
/// CopilotController is its own controller rather than living on Initiatives or
/// Contributions.
/// </remarks>
[ApiController]
[Authorize]
[Route("api/initiatives/{initiativeId:int}/content-generation")]
public class ContentGenerationController : ControllerBase
{
    private readonly IContentGenerationService _contentGenerationService;
    private readonly ILogger<ContentGenerationController> _logger;

    public ContentGenerationController(
        IContentGenerationService contentGenerationService,
        ILogger<ContentGenerationController> logger)
    {
        _contentGenerationService = contentGenerationService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Generate(
        int initiativeId,
        [FromBody] ContentGenerationRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var generated = await _contentGenerationService.GenerateAsync(
                initiativeId, request, cancellationToken);

            return Ok(generated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            _logger.LogInformation("Content generation rejected: {Reason}", ex.Message);

            return BadRequest(new { message = ex.Message });
        }
        catch (CopilotException)
        {
            // Deliberately not logging the caught exception or including its message in
            // the response: it can carry the provider's own diagnostic text, which is
            // more than should reach either a log or a client here. A generic, retryable
            // message and failure category are all that's needed on either side.
            _logger.LogWarning(
                "Content generation failed for Initiative {InitiativeId}, format {Format}.",
                initiativeId, request.Format);

            return Problem(
                title: "Content generation is unavailable",
                detail: "Content generation is temporarily unavailable. Please try again.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
