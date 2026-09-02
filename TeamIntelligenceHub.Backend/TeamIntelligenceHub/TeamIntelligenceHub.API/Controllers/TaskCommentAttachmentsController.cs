using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Services;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.API.Controllers;

/// <summary>
/// Files attached to comments on a task.
/// </summary>
/// <remarks>
/// The browser posts the file here and the API streams it to Blob Storage, so the
/// container stays private and every read goes through an authenticated endpoint.
/// </remarks>
[ApiController]
[Authorize]
[Route("api/tasks/{taskId:int}")]
public class TaskCommentAttachmentsController : ControllerBase
{
    private readonly ITaskCommentAttachmentService _attachmentService;
    private readonly ILogger<TaskCommentAttachmentsController> _logger;

    public TaskCommentAttachmentsController(
        ITaskCommentAttachmentService attachmentService,
        ILogger<TaskCommentAttachmentsController> logger)
    {
        _attachmentService = attachmentService;
        _logger = logger;
    }

    /// <summary>Uploads one file against a comment.</summary>
    [HttpPost("comments/{commentId:int}/attachments")]
    [RequestSizeLimit(TaskCommentAttachment.MaxFileSizeBytes + 1024 * 1024)]
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

    /// <summary>Streams a stored file back through the API.</summary>
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
