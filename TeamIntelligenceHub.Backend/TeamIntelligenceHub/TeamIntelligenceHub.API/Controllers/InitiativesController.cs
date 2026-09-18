using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Services;

namespace TeamIntelligenceHub.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class InitiativesController : ControllerBase
{
    private readonly IInitiativeService _initiativeService;
    private readonly ILogger<InitiativesController> _logger;

    public InitiativesController(
        IInitiativeService initiativeService,
        ILogger<InitiativesController> logger)
    {
        _initiativeService = initiativeService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var initiatives = await _initiativeService.GetAllAsync();

        return Ok(initiatives);
    }

    /// <summary>
    /// Aggregate counts for the Insights page's Readiness and Audience &amp; Roles tabs.
    /// </summary>
    [HttpGet("insights")]
    public async Task<IActionResult> GetReadinessInsights()
    {
        return Ok(await _initiativeService.GetReadinessInsightsAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var initiative = await _initiativeService.GetByIdAsync(id);

        if (initiative == null)
        {
            return NotFound();
        }

        return Ok(initiative);
    }

    /// <summary>
    /// Creates an Initiative. The caller owns it unless another owner is supplied.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateInitiativeRequestDto request)
    {
        try
        {
            var created = await _initiativeService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);
        }
        catch (ValidationException ex)
        {
            // A business-rule failure, not a server fault. The message is written for
            // the person filling in the form, so pass it through.
            _logger.LogInformation(
                "Initiative creation rejected: {Reason}", ex.Message);

            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Replaces the whole Initiative. Open to any signed-in user — there is no
    /// per-Initiative ownership check on this endpoint.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id, [FromBody] UpdateInitiativeRequestDto request)
    {
        try
        {
            var updated = await _initiativeService.UpdateAsync(id, request);

            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            _logger.LogInformation(
                "Initiative update rejected: {Reason}", ex.Message);

            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// What deleting this Initiative would also remove — Contributions, Tasks, Activity,
    /// Team members — for a confirmation dialog before Remove is actually called.
    /// </summary>
    [HttpGet("{id:int}/deletion-impact")]
    public async Task<IActionResult> GetDeletionImpact(int id)
    {
        try
        {
            return Ok(await _initiativeService.GetDeletionImpactAsync(id));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes the Initiative and cascades through its Contributions, Tasks, Activity,
    /// and Team members. Open to any signed-in user.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _initiativeService.RemoveAsync(id, cancellationToken);

            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
