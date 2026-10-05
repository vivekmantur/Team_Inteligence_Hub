using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// Uploads, downloads, and removes files attached to a contribution.
/// </summary>
/// <remarks>
/// FileUpload and FileDownload are declared alongside ITaskCommentAttachmentService and
/// reused here rather than duplicated. They carry no web types, so the Application layer
/// stays free of ASP.NET.
/// </remarks>
public interface IContributionAttachmentService
{
    /// <summary>
    /// Stores a file on a contribution the caller submitted and queues it for document
    /// extraction. Throws NotFoundException for an unknown contribution and
    /// ValidationException for someone else's contribution or a blocked, empty, or
    /// oversized file.
    /// </summary>
    Task<ContributionAttachmentDto> UploadAsync(
        int contributionId,
        FileUpload upload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens an attachment for streaming. Throws NotFoundException when the attachment
    /// does not belong to this contribution.
    /// </summary>
    Task<FileDownload> DownloadAsync(
        int contributionId,
        int attachmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an attachment row and its stored file. Throws NotFoundException when the
    /// attachment does not belong to this contribution and ValidationException when the
    /// caller did not submit the contribution.
    /// </summary>
    Task RemoveAsync(
        int contributionId,
        int attachmentId,
        CancellationToken cancellationToken = default);
}
