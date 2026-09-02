using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Services;

namespace TeamIntelligenceHub.API.Controllers;

/// <summary>
/// The activity feed for one Initiative.
/// </summary>
/// <remarks>
/// Posting with AutoCreateTaskEnabled raises a task for each person mentioned, so a
/// single POST here can create several rows in Tasks. The response lists them.
/// </remarks>
[ApiController]
[Authorize]
[Route("api/initiatives/{initiativeId:int}/activity")]
public class ActivityController : ControllerBase
{
    private readonly IActivityService _activityService;
    private readonly ILogger<ActivityController> _logger;

    public ActivityController(
        IActivityService activityService,
        ILogger<ActivityController> logger)
    {
        _activityService = activityService;
        _logger = logger;
    }

    /// <summary>Returns the feed, newest first.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(int initiativeId)
    {
        try
        {
            var activities = await _activityService.GetByInitiativeAsync(initiativeId);

            return Ok(activities);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        int initiativeId,
        [FromBody] CreateActivityRequestDto request)
    {
        try
        {
            var created = await _activityService.CreateAsync(initiativeId, request);

            return CreatedAtAction(nameof(GetAll), new { initiativeId }, created);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            _logger.LogInformation("Activity post rejected: {Reason}", ex.Message);

            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPut("{activityId:int}")]
    public async Task<IActionResult> Update(
        int initiativeId,
        int activityId,
        [FromBody] UpdateActivityRequestDto request)
    {
        try
        {
            var updated = await _activityService.UpdateAsync(
                initiativeId, activityId, request);

            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes the post. Tasks it raised are kept and detached from it.
    /// </summary>
    [HttpDelete("{activityId:int}")]
    public async Task<IActionResult> Remove(int initiativeId, int activityId)
    {
        try
        {
            await _activityService.RemoveAsync(initiativeId, activityId);

            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
