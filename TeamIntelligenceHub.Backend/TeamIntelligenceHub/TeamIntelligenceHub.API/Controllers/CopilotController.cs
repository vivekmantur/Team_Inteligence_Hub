using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Services;

namespace TeamIntelligenceHub.API.Controllers;

/// <summary>
/// Answers questions grounded on the indexed corpus in Azure AI Search.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CopilotController : ControllerBase
{
    private readonly ICopilotService _copilotService;
    private readonly ILogger<CopilotController> _logger;

    public CopilotController(
        ICopilotService copilotService,
        ILogger<CopilotController> logger)
    {
        _copilotService = copilotService;
        _logger = logger;
    }

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
