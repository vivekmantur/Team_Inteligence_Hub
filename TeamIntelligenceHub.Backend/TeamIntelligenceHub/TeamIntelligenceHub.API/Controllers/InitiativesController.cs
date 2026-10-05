// 1. Get all initiatives
// 2. Get readiness insights across initiatives
// 3. Get one initiative by ID
// 4. Create an initiative
// 5. Update an initiative
// 6. Get what deleting an initiative would remove
// 7. Delete an initiative

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Services;

namespace TeamIntelligenceHub.API.Controllers;

/// <summary>Lists, reads, creates, updates, and deletes Initiatives, and serves their readiness insights.</summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class InitiativesController : ControllerBase
{
    private readonly IInitiativeService _initiativeService;
    private readonly ILogger<InitiativesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InitiativesController"/> class.
    /// </summary>
    /// <param name="initiativeService">The service that reads and writes Initiatives.</param>
    /// <param name="logger">The logger used to record rejected requests.</param>
    public InitiativesController(
        IInitiativeService initiativeService,
        ILogger<InitiativesController> logger)
    {
        _initiativeService = initiativeService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all Initiatives.
    /// </summary>
    /// <returns>200 OK with the Initiatives.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var initiatives = await _initiativeService.GetAllAsync();

        return Ok(initiatives);
    }

    /// <summary>
    /// Gets the aggregate counts for the Insights page's Readiness and Audience &amp; Roles tabs.
    /// </summary>
    /// <returns>200 OK with the readiness insights.</returns>
    [HttpGet("insights")]
    public async Task<IActionResult> GetReadinessInsights()
    {
        return Ok(await _initiativeService.GetReadinessInsightsAsync());
    }

    /// <summary>
    /// Gets the Initiative with the given ID.
    /// </summary>
    /// <param name="id">The Initiative identifier.</param>
    /// <returns>200 OK with the Initiative, or 404 Not Found when it does not exist.</returns>
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
    /// <param name="request">The details of the Initiative to create.</param>
    /// <returns>201 Created with the new Initiative, 400 Bad Request when validation fails, or 401 Unauthorized when the caller cannot be identified.</returns>
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
    /// Replaces the whole Initiative. Open to any signed-in user, with no per-Initiative ownership check on this endpoint.
    /// </summary>
    /// <param name="id">The Initiative identifier.</param>
    /// <param name="request">The full updated Initiative.</param>
    /// <returns>200 OK with the updated Initiative, 404 Not Found when it does not exist, or 400 Bad Request when validation fails.</returns>
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
    /// Gets the Contributions, Tasks, Activity, and Team members that deleting this Initiative would also remove, for a confirmation dialog shown before Remove is called.
    /// </summary>
    /// <param name="id">The Initiative identifier.</param>
    /// <returns>200 OK with the deletion impact counts, or 404 Not Found when the Initiative does not exist.</returns>
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
    /// Deletes the Initiative and cascades through its Contributions, Tasks, Activity, and Team members. Open to any signed-in user.
    /// </summary>
    /// <param name="id">The Initiative identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>204 No Content when the Initiative is deleted, or 404 Not Found when it does not exist.</returns>
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
