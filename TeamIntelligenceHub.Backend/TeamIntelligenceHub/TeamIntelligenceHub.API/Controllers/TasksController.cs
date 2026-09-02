using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Services;

namespace TeamIntelligenceHub.API.Controllers;

/// <summary>
/// Tasks belonging to one Initiative.
/// </summary>
/// <remarks>
/// Nested under the Initiative, so the parent is fixed by the URL and a request cannot
/// place a task on a different Initiative than the one it was posted to.
/// </remarks>
[ApiController]
[Authorize]
[Route("api/initiatives/{initiativeId:int}/tasks")]
public class TasksController : ControllerBase
{
    private readonly IInitiativeTaskService _taskService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(
        IInitiativeTaskService taskService,
        ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int initiativeId)
    {
        try
        {
            var tasks = await _taskService.GetByInitiativeAsync(initiativeId);

            return Ok(tasks);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{taskId:int}")]
    public async Task<IActionResult> GetById(int initiativeId, int taskId)
    {
        try
        {
            var task = await _taskService.GetByIdAsync(initiativeId, taskId);

            return Ok(task);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        int initiativeId,
        [FromBody] CreateTaskRequestDto request)
    {
        try
        {
            var created = await _taskService.CreateAsync(initiativeId, request);

            return CreatedAtAction(
                nameof(GetById),
                new { initiativeId, taskId = created.Id },
                created);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            _logger.LogInformation("Task creation rejected: {Reason}", ex.Message);

            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPut("{taskId:int}")]
    public async Task<IActionResult> Update(
        int initiativeId,
        int taskId,
        [FromBody] UpdateTaskRequestDto request)
    {
        try
        {
            var updated = await _taskService.UpdateAsync(initiativeId, taskId, request);

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

    [HttpDelete("{taskId:int}")]
    public async Task<IActionResult> Remove(int initiativeId, int taskId)
    {
        try
        {
            await _taskService.RemoveAsync(initiativeId, taskId);

            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
