using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Services;

namespace TeamIntelligenceHub.API.Controllers;

/// <summary>
/// Team membership for one Initiative.
/// </summary>
/// <remarks>
/// Nested under the Initiative because a membership has no meaning without it. The
/// Initiative id comes from the route, so a request cannot post to one Initiative while
/// claiming to belong to another.
/// </remarks>
[ApiController]
[Authorize]
[Route("api/initiatives/{initiativeId:int}/members")]
public class InitiativeMembersController : ControllerBase
{
    private readonly IInitiativeMemberService _memberService;
    private readonly ILogger<InitiativeMembersController> _logger;

    public InitiativeMembersController(
        IInitiativeMemberService memberService,
        ILogger<InitiativeMembersController> logger)
    {
        _memberService = memberService;
        _logger = logger;
    }

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
