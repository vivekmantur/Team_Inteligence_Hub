using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Services;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.API.Controllers;

/// <summary>
/// Files attached to a contribution.
/// </summary>
/// <remarks>
/// The browser posts the file here and the API streams it to Blob Storage, so the
/// container stays private and every read goes through an authenticated endpoint.
///
/// Separate from the create call on purpose: a file needs a ContributionId to hang off.
/// The wizard's Save draft button exists to produce one, so uploads have somewhere to go
/// before the contribution is submitted.
/// </remarks>
[ApiController]
[Authorize]
[Route("api/contributions/{contributionId:int}")]
public class ContributionAttachmentsController : ControllerBase
{
    private readonly IContributionAttachmentService _attachmentService;
    private readonly ILogger<ContributionAttachmentsController> _logger;

    public ContributionAttachmentsController(
        IContributionAttachmentService attachmentService,
        ILogger<ContributionAttachmentsController> logger)
    {
        _attachmentService = attachmentService;
        _logger = logger;
    }

    /// <summary>Uploads one file against a contribution.</summary>
    [HttpPost("attachments")]
    [RequestSizeLimit(ContributionAttachment.MaxFileSizeBytes + 1024 * 1024)]
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

    /// <summary>Streams a stored file back through the API.</summary>
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
