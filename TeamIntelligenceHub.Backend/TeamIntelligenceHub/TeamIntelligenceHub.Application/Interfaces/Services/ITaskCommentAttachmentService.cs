using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>What the caller hands over for an upload, free of any web types.</summary>
public record FileUpload(
    Stream Content,
    string FileName,
    string ContentType,
    long Length);

/// <summary>What comes back when a file is read, ready to stream to the client.</summary>
public record FileDownload(
    Stream Content,
    string FileName,
    string ContentType);

/// <summary>
/// Uploads, downloads, and removes files attached to task comments.
/// </summary>
public interface ITaskCommentAttachmentService
{
    /// <summary>
    /// Stores a file on the caller's own comment. Throws NotFoundException when the
    /// comment is not on this task and ValidationException for someone else's comment or
    /// a blocked, empty, or oversized file.
    /// </summary>
    Task<TaskCommentAttachmentDto> UploadAsync(
        int taskId,
        int commentId,
        FileUpload upload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens an attachment for streaming. Throws NotFoundException when the attachment is
    /// not on this task.
    /// </summary>
    Task<FileDownload> DownloadAsync(
        int taskId,
        int attachmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an attachment row and its stored file. Throws NotFoundException when the
    /// attachment is not on this task and ValidationException when the comment is not the
    /// caller's.
    /// </summary>
    Task RemoveAsync(
        int taskId,
        int attachmentId,
        CancellationToken cancellationToken = default);
}
