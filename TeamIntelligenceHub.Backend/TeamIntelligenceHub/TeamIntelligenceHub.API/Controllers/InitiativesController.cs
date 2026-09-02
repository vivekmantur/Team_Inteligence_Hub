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
}
