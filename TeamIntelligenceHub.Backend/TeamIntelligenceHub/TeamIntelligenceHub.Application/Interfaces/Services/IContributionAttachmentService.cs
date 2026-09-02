using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <remarks>
/// FileUpload and FileDownload are declared alongside ITaskCommentAttachmentService and
/// reused here rather than duplicated. They carry no web types, so the Application layer
/// stays free of ASP.NET.
/// </remarks>
public interface IContributionAttachmentService
{
    Task<ContributionAttachmentDto> UploadAsync(
        int contributionId,
        FileUpload upload,
        CancellationToken cancellationToken = default);

    Task<FileDownload> DownloadAsync(
        int contributionId,
        int attachmentId,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        int contributionId,
        int attachmentId,
        CancellationToken cancellationToken = default);
}
