// 1. Get all tasks for an initiative
// 2. Get one task by ID
// 3. Create a task
// 4. Update a task
// 5. Delete a task

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Services;

namespace TeamIntelligenceHub.API.Controllers;

/// <summary>
/// Manages the tasks belonging to one Initiative. The URL fixes the parent, so a request
/// cannot place a task on a different Initiative.
/// </summary>
[ApiController]
[Authorize]
[Route("api/initiatives/{initiativeId:int}/tasks")]
public class TasksController : ControllerBase
{
    private readonly IInitiativeTaskService _taskService;
    private readonly ILogger<TasksController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TasksController"/> class.
    /// </summary>
    /// <param name="taskService">The service that handles task operations.</param>
    /// <param name="logger">The logger used to record rejected tasks.</param>
    public TasksController(
        IInitiativeTaskService taskService,
        ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the Initiative's tasks.
    /// </summary>
    /// <param name="initiativeId">The Initiative whose tasks are returned.</param>
    /// <returns>200 OK with the tasks, or 404 Not Found when the Initiative does not exist.</returns>
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

    /// <summary>
    /// Gets the task with the given ID within an Initiative.
    /// </summary>
    /// <param name="initiativeId">The Initiative that owns the task.</param>
    /// <param name="taskId">The task identifier.</param>
    /// <returns>200 OK with the task, or 404 Not Found when the Initiative or task does not exist.</returns>
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

    /// <summary>
    /// Creates a task on the Initiative.
    /// </summary>
    /// <param name="initiativeId">The Initiative the task belongs to.</param>
    /// <param name="request">The details of the task to create.</param>
    /// <returns>201 Created with the new task, 404 Not Found when the Initiative does not exist, 400 Bad Request when validation fails, or 401 Unauthorized when the caller cannot be identified.</returns>
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

    /// <summary>
    /// Updates a task on the Initiative.
    /// </summary>
    /// <param name="initiativeId">The Initiative that owns the task.</param>
    /// <param name="taskId">The task identifier.</param>
    /// <param name="request">The updated task details.</param>
    /// <returns>200 OK with the updated task, 404 Not Found when the Initiative or task does not exist, or 400 Bad Request when validation fails.</returns>
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

    /// <summary>
    /// Deletes a task from the Initiative.
    /// </summary>
    /// <param name="initiativeId">The Initiative that owns the task.</param>
    /// <param name="taskId">The task identifier.</param>
    /// <returns>204 No Content when the task is deleted, or 404 Not Found when the Initiative or task does not exist.</returns>
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
