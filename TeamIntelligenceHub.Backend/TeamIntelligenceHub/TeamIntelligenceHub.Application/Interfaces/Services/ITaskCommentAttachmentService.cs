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

public interface ITaskCommentAttachmentService
{
    Task<TaskCommentAttachmentDto> UploadAsync(
        int taskId,
        int commentId,
        FileUpload upload,
        CancellationToken cancellationToken = default);

    Task<FileDownload> DownloadAsync(
        int taskId,
        int attachmentId,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        int taskId,
        int attachmentId,
        CancellationToken cancellationToken = default);
}
