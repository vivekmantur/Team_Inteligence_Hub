// 1. Ask Copilot a question

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Services;

namespace TeamIntelligenceHub.API.Controllers;

/// <summary>
/// Answers questions grounded on the indexed search corpus.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CopilotController : ControllerBase
{
    private readonly ICopilotService _copilotService;
    private readonly ILogger<CopilotController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotController"/> class.
    /// </summary>
    /// <param name="copilotService">The service that searches the corpus and generates the answer.</param>
    /// <param name="logger">The logger used to record provider failures.</param>
    public CopilotController(
        ICopilotService copilotService,
        ILogger<CopilotController> logger)
    {
        _copilotService = copilotService;
        _logger = logger;
    }

    /// <summary>
    /// Answers a question grounded on the indexed search corpus.
    /// </summary>
    /// <param name="request">The question to answer.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>200 OK with the answer, 400 Bad Request when validation fails, or 502 Bad Gateway when a Copilot provider is unavailable.</returns>
    [HttpPost("ask")]
    public async Task<IActionResult> Ask(
        [FromBody] CopilotQuestionRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var answer = await _copilotService.AskAsync(request.Question, cancellationToken);

            return Ok(answer);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (CopilotException ex)
        {
            // Embedding, search, or chat provider misconfigured or unreachable. Report
            // the reason rather than an opaque 500 — this is the failure people actually
            // hit while setting up.
            _logger.LogError(ex, "Copilot request failed");

            return Problem(
                title: "Copilot is unavailable",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
