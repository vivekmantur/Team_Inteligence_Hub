using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// What the caller hands over for an upload, free of any web types.
/// </summary>
/// <param name="Content">The file bytes.</param>
/// <param name="FileName">The name the client gave the file.</param>
/// <param name="ContentType">The MIME type the client reported.</param>
/// <param name="Length">The file size in bytes.</param>
public record FileUpload(
    Stream Content,
    string FileName,
    string ContentType,
    long Length);

/// <summary>
/// What comes back when a file is read, ready to stream to the client.
/// </summary>
/// <param name="Content">The stream of the stored file.</param>
/// <param name="FileName">The original file name, for the download header.</param>
/// <param name="ContentType">The stored MIME type.</param>
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
    /// Stores a file on the caller's own comment.
    /// </summary>
    /// <param name="taskId">The task the comment is on.</param>
    /// <param name="commentId">The comment to attach the file to.</param>
    /// <param name="upload">The file content, name, type, and length.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The stored attachment's metadata.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the comment is not on this task.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown for someone else's comment or a blocked, empty, or oversized file.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the token carries no Entra object id.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.FileStorageException">
    /// Thrown when the file store rejects the upload.
    /// </exception>
    Task<TaskCommentAttachmentDto> UploadAsync(
        int taskId,
        int commentId,
        FileUpload upload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens an attachment for streaming.
    /// </summary>
    /// <param name="taskId">The task the attachment's comment is on.</param>
    /// <param name="attachmentId">The attachment to open.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The file stream with its original name and content type.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the attachment is not on this task.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.FileStorageException">
    /// Thrown when the file store cannot read the file.
    /// </exception>
    Task<FileDownload> DownloadAsync(
        int taskId,
        int attachmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an attachment row and its stored file.
    /// </summary>
    /// <param name="taskId">The task the attachment's comment is on.</param>
    /// <param name="attachmentId">The attachment to delete.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the attachment is not on this task.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown when the comment is not the caller's.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the token carries no Entra object id.
    /// </exception>
    Task RemoveAsync(
        int taskId,
        int attachmentId,
        CancellationToken cancellationToken = default);
}
