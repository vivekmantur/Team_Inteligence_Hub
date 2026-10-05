// 1. Get the comment thread for a task
// 2. Add a comment to a task
// 3. Update a comment
// 4. Delete a comment and its replies

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Services;

namespace TeamIntelligenceHub.API.Controllers;

/// <summary>
/// Handles the comment thread on one task. It nests under the task alone, because a task
/// id is unique on its own.
/// </summary>
[ApiController]
[Authorize]
[Route("api/tasks/{taskId:int}/comments")]
public class TaskCommentsController : ControllerBase
{
    private readonly ITaskCommentService _commentService;
    private readonly ILogger<TaskCommentsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskCommentsController"/> class.
    /// </summary>
    /// <param name="commentService">The service that reads and writes task comments.</param>
    /// <param name="logger">The logger used to record rejected comments.</param>
    public TaskCommentsController(
        ITaskCommentService commentService,
        ILogger<TaskCommentsController> logger)
    {
        _commentService = commentService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the whole comment thread, oldest first. Replies carry ParentCommentId so the client can nest them.
    /// </summary>
    /// <param name="taskId">The task whose comments are returned.</param>
    /// <returns>200 OK with the comments, or 404 Not Found when the task does not exist.</returns>
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

    /// <summary>
    /// Adds a comment or reply to the task.
    /// </summary>
    /// <param name="taskId">The task the comment is posted on.</param>
    /// <param name="request">The comment text, mentioned users, and optional parent comment.</param>
    /// <returns>201 Created with the new comment, 404 Not Found when the task does not exist, 400 Bad Request when validation fails, or 401 Unauthorized when the caller cannot be identified.</returns>
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

    /// <summary>
    /// Updates a comment on the task.
    /// </summary>
    /// <param name="taskId">The task that owns the comment.</param>
    /// <param name="commentId">The comment identifier.</param>
    /// <param name="request">The updated comment text and mentioned users.</param>
    /// <returns>200 OK with the updated comment, 404 Not Found when the task or comment does not exist, or 400 Bad Request when validation fails.</returns>
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

    /// <summary>
    /// Deletes the comment and any replies underneath it.
    /// </summary>
    /// <param name="taskId">The task that owns the comment.</param>
    /// <param name="commentId">The comment identifier.</param>
    /// <returns>204 No Content when the comment is deleted, 404 Not Found when the task or comment does not exist, or 400 Bad Request when validation fails.</returns>
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
