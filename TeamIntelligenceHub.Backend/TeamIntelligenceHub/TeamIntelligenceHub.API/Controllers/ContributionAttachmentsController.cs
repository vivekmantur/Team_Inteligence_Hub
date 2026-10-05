// 1. Upload a file to a contribution
// 2. Download a contribution attachment
// 3. Delete a contribution attachment

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Services;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.API.Controllers;

/// <summary>
/// Handles the files attached to a contribution. The API streams files to private
/// storage, so every read goes through an authenticated endpoint.
/// </summary>
[ApiController]
[Authorize]
[Route("api/contributions/{contributionId:int}")]
public class ContributionAttachmentsController : ControllerBase
{
    /// <summary>Extra request-body allowance on top of the file limit, for multipart framing.</summary>
    private const long MultipartOverheadBytes = 1024 * 1024;

    private readonly IContributionAttachmentService _attachmentService;
    private readonly ILogger<ContributionAttachmentsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContributionAttachmentsController"/> class.
    /// </summary>
    /// <param name="attachmentService">The service that stores, reads, and deletes contribution attachments.</param>
    /// <param name="logger">The logger used to record rejected files and storage failures.</param>
    public ContributionAttachmentsController(
        IContributionAttachmentService attachmentService,
        ILogger<ContributionAttachmentsController> logger)
    {
        _attachmentService = attachmentService;
        _logger = logger;
    }

    /// <summary>
    /// Uploads one file against a contribution.
    /// </summary>
    /// <param name="contributionId">The contribution the file is attached to.</param>
    /// <param name="file">The uploaded file.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>200 OK with the new attachment, 400 Bad Request when no file is sent or validation fails, 404 Not Found when the contribution does not exist, 401 Unauthorized when the caller cannot be identified, or 502 Bad Gateway when file storage is unavailable.</returns>
    [HttpPost("attachments")]
    [RequestSizeLimit(ContributionAttachment.MaxFileSizeBytes + MultipartOverheadBytes)]
    public async Task<IActionResult> Upload(
        int contributionId,
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
                contributionId,
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
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (FileStorageException ex)
        {
            // Storage misconfigured or unreachable. Report the reason rather than an
            // opaque 500 — this is the failure people actually hit while setting up.
            _logger.LogError(ex, "Contribution attachment upload failed");

            return Problem(
                title: "Attachment storage unavailable",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    /// <summary>
    /// Streams a stored contribution attachment back through the API.
    /// </summary>
    /// <param name="contributionId">The contribution that owns the attachment.</param>
    /// <param name="attachmentId">The attachment identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>200 OK with the file content, content type, and file name, 404 Not Found when the contribution or attachment does not exist, or 502 Bad Gateway when file storage is unavailable.</returns>
    [HttpGet("attachments/{attachmentId:int}/download")]
    public async Task<IActionResult> Download(
        int contributionId,
        int attachmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var download = await _attachmentService.DownloadAsync(
                contributionId, attachmentId, cancellationToken);

            return File(download.Content, download.ContentType, download.FileName);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (FileStorageException ex)
        {
            _logger.LogError(ex, "Contribution attachment download failed");

            return Problem(
                title: "Attachment storage unavailable",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    /// <summary>
    /// Deletes an attachment from the contribution.
    /// </summary>
    /// <param name="contributionId">The contribution that owns the attachment.</param>
    /// <param name="attachmentId">The attachment identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>204 No Content when the attachment is deleted, 404 Not Found when the contribution or attachment does not exist, 400 Bad Request when validation fails, or 401 Unauthorized when the caller cannot be identified.</returns>
    [HttpDelete("attachments/{attachmentId:int}")]
    public async Task<IActionResult> Remove(
        int contributionId,
        int attachmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _attachmentService.RemoveAsync(
                contributionId, attachmentId, cancellationToken);

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
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
