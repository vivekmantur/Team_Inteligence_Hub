// 1. Upload a file to a task comment
// 2. Download a task comment attachment
// 3. Delete a task comment attachment

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Services;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.API.Controllers;

/// <summary>
/// Handles the files attached to comments on a task. The API streams files to private
/// storage, so every read goes through an authenticated endpoint.
/// </summary>
[ApiController]
[Authorize]
[Route("api/tasks/{taskId:int}")]
public class TaskCommentAttachmentsController : ControllerBase
{
    /// <summary>Extra request-body allowance on top of the file limit, for multipart framing.</summary>
    private const long MultipartOverheadBytes = 1024 * 1024;

    private readonly ITaskCommentAttachmentService _attachmentService;
    private readonly ILogger<TaskCommentAttachmentsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskCommentAttachmentsController"/> class.
    /// </summary>
    /// <param name="attachmentService">The service that stores, reads, and deletes comment attachments.</param>
    /// <param name="logger">The logger used to record rejected files and storage failures.</param>
    public TaskCommentAttachmentsController(
        ITaskCommentAttachmentService attachmentService,
        ILogger<TaskCommentAttachmentsController> logger)
    {
        _attachmentService = attachmentService;
        _logger = logger;
    }

    /// <summary>
    /// Uploads one file against a comment on the task.
    /// </summary>
    /// <param name="taskId">The task that owns the comment.</param>
    /// <param name="commentId">The comment the file is attached to.</param>
    /// <param name="file">The uploaded file.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>200 OK with the new attachment, 400 Bad Request when no file is sent or validation fails, 404 Not Found when the task or comment does not exist, or 502 Bad Gateway when file storage is unavailable.</returns>
    [HttpPost("comments/{commentId:int}/attachments")]
    [RequestSizeLimit(TaskCommentAttachment.MaxFileSizeBytes + MultipartOverheadBytes)]
    public async Task<IActionResult> Upload(
        int taskId,
        int commentId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "No file was uploaded." });
        }

        try
        {
            await using var stream = file.OpenReadStream();

            var created = await _attachmentService.UploadAsync(
                taskId,
                commentId,
                new FileUpload(stream, file.FileName, file.ContentType, file.Length),
                cancellationToken);

            return Ok(created);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            _logger.LogInformation("Attachment rejected: {Reason}", ex.Message);

            return BadRequest(new { message = ex.Message });
        }
        catch (FileStorageException ex)
        {
            // Storage misconfigured or unreachable. Report the reason rather than an
            // opaque 500 — this is the failure people actually hit while setting up.
            _logger.LogError(ex, "Attachment upload failed");

            return Problem(
                title: "Attachment storage unavailable",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    /// <summary>
    /// Streams a stored comment attachment back through the API.
    /// </summary>
    /// <param name="taskId">The task that owns the attachment.</param>
    /// <param name="attachmentId">The attachment identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>200 OK with the file content, content type, and file name, 404 Not Found when the task or attachment does not exist, or 502 Bad Gateway when file storage is unavailable.</returns>
    [HttpGet("attachments/{attachmentId:int}/download")]
    public async Task<IActionResult> Download(
        int taskId,
        int attachmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var download = await _attachmentService.DownloadAsync(
                taskId, attachmentId, cancellationToken);

            return File(download.Content, download.ContentType, download.FileName);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (FileStorageException ex)
        {
            _logger.LogError(ex, "Attachment download failed");

            return Problem(
                title: "Attachment storage unavailable",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    /// <summary>
    /// Deletes an attachment from one of the task's comments.
    /// </summary>
    /// <param name="taskId">The task that owns the attachment.</param>
    /// <param name="attachmentId">The attachment identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>204 No Content when the attachment is deleted, 404 Not Found when the task or attachment does not exist, or 400 Bad Request when validation fails.</returns>
    [HttpDelete("attachments/{attachmentId:int}")]
    public async Task<IActionResult> Remove(
        int taskId,
        int attachmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _attachmentService.RemoveAsync(taskId, attachmentId, cancellationToken);

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
