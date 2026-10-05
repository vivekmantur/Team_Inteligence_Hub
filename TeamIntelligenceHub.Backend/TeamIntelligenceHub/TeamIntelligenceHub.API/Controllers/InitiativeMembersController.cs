// 1. Get all team members for an initiative
// 2. Add a team member to an initiative
// 3. Update a team member
// 4. Remove a team member

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Services;

namespace TeamIntelligenceHub.API.Controllers;

/// <summary>
/// Manages team membership for one Initiative. The Initiative id comes from the route, so
/// a request cannot claim to belong to a different Initiative.
/// </summary>
[ApiController]
[Authorize]
[Route("api/initiatives/{initiativeId:int}/members")]
public class InitiativeMembersController : ControllerBase
{
    private readonly IInitiativeMemberService _memberService;
    private readonly ILogger<InitiativeMembersController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InitiativeMembersController"/> class.
    /// </summary>
    /// <param name="memberService">The service that reads and writes Initiative memberships.</param>
    /// <param name="logger">The logger used to record rejected requests.</param>
    public InitiativeMembersController(
        IInitiativeMemberService memberService,
        ILogger<InitiativeMembersController> logger)
    {
        _memberService = memberService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the Initiative's team members.
    /// </summary>
    /// <param name="initiativeId">The Initiative whose members are returned.</param>
    /// <returns>200 OK with the team members, or 404 Not Found when the Initiative does not exist.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(int initiativeId)
    {
        try
        {
            var members = await _memberService.GetByInitiativeAsync(initiativeId);

            return Ok(members);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Adds a team member to the Initiative.
    /// </summary>
    /// <param name="initiativeId">The Initiative the member joins.</param>
    /// <param name="request">The user to add and their role on the Initiative.</param>
    /// <returns>201 Created with the new membership, 404 Not Found when the Initiative or user does not exist, or 400 Bad Request when validation fails.</returns>
    [HttpPost]
    public async Task<IActionResult> Add(
        int initiativeId,
        [FromBody] AddInitiativeMemberRequestDto request)
    {
        try
        {
            var created = await _memberService.AddAsync(initiativeId, request);

            return CreatedAtAction(
                nameof(GetAll),
                new { initiativeId },
                created);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            _logger.LogInformation(
                "Add team member rejected: {Reason}", ex.Message);

            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Updates a team member on the Initiative.
    /// </summary>
    /// <param name="initiativeId">The Initiative that owns the membership.</param>
    /// <param name="memberId">The membership identifier.</param>
    /// <param name="request">The updated role, responsibility area, and allocation.</param>
    /// <returns>200 OK with the updated membership, 404 Not Found when the Initiative or membership does not exist, or 400 Bad Request when validation fails.</returns>
    [HttpPut("{memberId:int}")]
    public async Task<IActionResult> Update(
        int initiativeId,
        int memberId,
        [FromBody] UpdateInitiativeMemberRequestDto request)
    {
        try
        {
            var updated = await _memberService.UpdateAsync(
                initiativeId, memberId, request);

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
    /// Removes a team member from the Initiative.
    /// </summary>
    /// <param name="initiativeId">The Initiative that owns the membership.</param>
    /// <param name="memberId">The membership identifier.</param>
    /// <returns>204 No Content when the member is removed, or 404 Not Found when the Initiative or membership does not exist.</returns>
    [HttpDelete("{memberId:int}")]
    public async Task<IActionResult> Remove(int initiativeId, int memberId)
    {
        try
        {
            await _memberService.RemoveAsync(initiativeId, memberId);

            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
