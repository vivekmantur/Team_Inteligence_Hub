// 1. Get the activity feed for an initiative
// 2. Post an activity to an initiative's feed
// 3. Update an activity post
// 4. Delete an activity post

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Services;

namespace TeamIntelligenceHub.API.Controllers;

/// <summary>
/// Handles the activity feed for one Initiative. A single post can also create a task
/// for each person it mentions.
/// </summary>
[ApiController]
[Authorize]
[Route("api/initiatives/{initiativeId:int}/activity")]
public class ActivityController : ControllerBase
{
    private readonly IActivityService _activityService;
    private readonly ILogger<ActivityController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityController"/> class.
    /// </summary>
    /// <param name="activityService">The service that reads and writes activity posts.</param>
    /// <param name="logger">The logger used to record rejected posts.</param>
    public ActivityController(
        IActivityService activityService,
        ILogger<ActivityController> logger)
    {
        _activityService = activityService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the Initiative's activity feed, newest first.
    /// </summary>
    /// <param name="initiativeId">The Initiative whose feed is returned.</param>
    /// <returns>200 OK with the activity posts, or 404 Not Found when the Initiative does not exist.</returns>
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

    /// <summary>
    /// Posts an activity to the Initiative's feed, raising a task for each mentioned person when auto-create is enabled.
    /// </summary>
    /// <param name="initiativeId">The Initiative the activity is posted to.</param>
    /// <param name="request">The activity message, mentioned users, and auto-create setting.</param>
    /// <returns>201 Created with the new activity and any tasks it raised, 404 Not Found when the Initiative does not exist, 400 Bad Request when validation fails, or 401 Unauthorized when the caller cannot be identified.</returns>
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

    /// <summary>
    /// Updates an activity post on the Initiative's feed.
    /// </summary>
    /// <param name="initiativeId">The Initiative that owns the activity.</param>
    /// <param name="activityId">The activity identifier.</param>
    /// <param name="request">The updated activity message and mentioned users.</param>
    /// <returns>200 OK with the updated activity, 404 Not Found when the Initiative or activity does not exist, or 400 Bad Request when validation fails.</returns>
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
    /// Deletes the activity post. Tasks it raised are kept and detached from it.
    /// </summary>
    /// <param name="initiativeId">The Initiative that owns the activity.</param>
    /// <param name="activityId">The activity identifier.</param>
    /// <returns>204 No Content when the post is deleted, 404 Not Found when the Initiative or activity does not exist, or 400 Bad Request when validation fails.</returns>
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
