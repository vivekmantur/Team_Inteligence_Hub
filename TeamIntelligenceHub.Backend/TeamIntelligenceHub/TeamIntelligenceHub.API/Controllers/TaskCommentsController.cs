using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Services;

namespace TeamIntelligenceHub.API.Controllers;

/// <summary>
/// The discussion on one task.
/// </summary>
/// <remarks>
/// Nested under the task rather than under initiative/task, which would make the route
/// three levels deep for no gain — a task id is unique on its own.
/// </remarks>
[ApiController]
[Authorize]
[Route("api/tasks/{taskId:int}/comments")]
public class TaskCommentsController : ControllerBase
{
    private readonly ITaskCommentService _commentService;
    private readonly ILogger<TaskCommentsController> _logger;

    public TaskCommentsController(
        ITaskCommentService commentService,
        ILogger<TaskCommentsController> logger)
    {
        _commentService = commentService;
        _logger = logger;
    }

    /// <summary>
    /// Returns the whole thread, oldest first. Replies carry ParentCommentId so the
    /// client can nest them.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(int taskId)
    {
        try
        {
            var comments = await _commentService.GetByTaskAsync(taskId);

            return Ok(comments);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        int taskId,
        [FromBody] CreateTaskCommentRequestDto request)
    {
        try
        {
            var created = await _commentService.CreateAsync(taskId, request);

            return CreatedAtAction(nameof(GetAll), new { taskId }, created);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            _logger.LogInformation("Comment rejected: {Reason}", ex.Message);

            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPut("{commentId:int}")]
    public async Task<IActionResult> Update(
        int taskId,
        int commentId,
        [FromBody] UpdateTaskCommentRequestDto request)
    {
        try
        {
            var updated = await _commentService.UpdateAsync(taskId, commentId, request);

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

    /// <summary>Deletes the comment and any replies underneath it.</summary>
    [HttpDelete("{commentId:int}")]
    public async Task<IActionResult> Remove(int taskId, int commentId)
    {
        try
        {
            await _commentService.RemoveAsync(taskId, commentId);

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
