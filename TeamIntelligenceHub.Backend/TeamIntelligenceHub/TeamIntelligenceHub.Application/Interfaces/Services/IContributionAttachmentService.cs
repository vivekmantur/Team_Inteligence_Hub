using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// Uploads, downloads, and removes files attached to a contribution. Reuses the web-free
/// FileUpload and FileDownload records declared beside ITaskCommentAttachmentService.
/// </summary>
public interface IContributionAttachmentService
{
    /// <summary>
    /// Stores a file on a contribution the caller submitted and queues it for document
    /// extraction.
    /// </summary>
    /// <param name="contributionId">The contribution to attach the file to.</param>
    /// <param name="upload">The file content, name, type, and length.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The stored attachment's metadata.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the contribution does not exist.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown for someone else's contribution or a blocked, empty, or oversized file.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the token carries no Entra object id.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.FileStorageException">
    /// Thrown when the file store rejects the upload.
    /// </exception>
    Task<ContributionAttachmentDto> UploadAsync(
        int contributionId,
        FileUpload upload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens an attachment for streaming.
    /// </summary>
    /// <param name="contributionId">The contribution the attachment belongs to.</param>
    /// <param name="attachmentId">The attachment to open.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The file stream with its original name and content type.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the attachment does not belong to this contribution.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.FileStorageException">
    /// Thrown when the file store cannot read the file.
    /// </exception>
    Task<FileDownload> DownloadAsync(
        int contributionId,
        int attachmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an attachment row and its stored file.
    /// </summary>
    /// <param name="contributionId">The contribution the attachment belongs to.</param>
    /// <param name="attachmentId">The attachment to delete.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the attachment does not belong to this contribution.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown when the caller did not submit the contribution.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the token carries no Entra object id.
    /// </exception>
    Task RemoveAsync(
        int contributionId,
        int attachmentId,
        CancellationToken cancellationToken = default);
}
