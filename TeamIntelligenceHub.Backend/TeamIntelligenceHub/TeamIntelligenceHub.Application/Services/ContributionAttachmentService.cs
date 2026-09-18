using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Application.Interfaces.Services;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Services;

public class ContributionAttachmentService : IContributionAttachmentService
{
    /// <summary>
    /// Extensions refused outright. Same list as task comment attachments: a blocklist
    /// rather than an allowlist, because people attach all sorts of legitimate things
    /// but nobody needs to attach an executable.
    /// </summary>
    private static readonly HashSet<string> BlockedExtensions = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".bat", ".cmd", ".com", ".msi", ".scr",
        ".ps1", ".psm1", ".sh", ".vbs", ".vbe", ".js", ".jse",
        ".jar", ".reg", ".lnk", ".hta", ".cpl"
    };

    private readonly IContributionAttachmentRepository _attachmentRepository;
    private readonly IContributionRepository _contributionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;
    private readonly IDocumentInsightExtractionQueue _extractionQueue;

    public ContributionAttachmentService(
        IContributionAttachmentRepository attachmentRepository,
        IContributionRepository contributionRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IFileStorage fileStorage,
        IDocumentInsightExtractionQueue extractionQueue)
    {
        _attachmentRepository = attachmentRepository;
        _contributionRepository = contributionRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
        _extractionQueue = extractionQueue;
    }

    public async Task<ContributionAttachmentDto> UploadAsync(
        int contributionId,
        FileUpload upload,
        CancellationToken cancellationToken = default)
    {
        var contribution = await RequireContributionAsync(contributionId);
        var caller = await GetCallerAsync();

        if (contribution.SubmittedByUserId != caller.Id)
        {
            throw new ValidationException(
                "You can only attach files to contributions you submitted.");
        }

        var fileName = ValidateFileName(upload.FileName);

        ValidateSize(upload.Length);

        var contentType = string.IsNullOrWhiteSpace(upload.ContentType)
            ? "application/octet-stream"
            : upload.ContentType.Trim();

        if (contentType.Length > ContributionAttachment.ContentTypeMaxLength)
        {
            contentType = contentType[..ContributionAttachment.ContentTypeMaxLength];
        }

        // Filed by entity so the container is browsable and everything belonging to one
        // contribution can be found, audited, or cleaned up as a unit.
        var blobName = await _fileStorage.UploadAsync(
            FileStorageArea.ContributionAttachments,
            upload.Content,
            fileName,
            contentType,
            $"contributions/{contributionId}",
            cancellationToken);

        try
        {
            var attachment = await _attachmentRepository.AddAsync(
                new ContributionAttachment
                {
                    ContributionId = contributionId,
                    FileName = fileName,
                    BlobName = blobName,
                    ContentType = contentType,
                    FileSize = upload.Length,
                    CreatedAt = DateTime.UtcNow
                });

            await _extractionQueue.EnqueueAsync(attachment.Id, cancellationToken);

            return MapToDto(attachment);
        }
        catch
        {
            // The bytes are already in storage. Without this the file would linger with
            // no row pointing at it, invisible and unbillable to anything.
            await _fileStorage.DeleteAsync(
                FileStorageArea.ContributionAttachments, blobName, cancellationToken);
            throw;
        }
    }

    public async Task<FileDownload> DownloadAsync(
        int contributionId,
        int attachmentId,
        CancellationToken cancellationToken = default)
    {
        var attachment = await RequireAttachmentAsync(contributionId, attachmentId);

        var content = await _fileStorage.DownloadAsync(
            FileStorageArea.ContributionAttachments,
            attachment.BlobName,
            cancellationToken);

        return new FileDownload(content, attachment.FileName, attachment.ContentType);
    }

    public async Task RemoveAsync(
        int contributionId,
        int attachmentId,
        CancellationToken cancellationToken = default)
    {
        var attachment = await RequireAttachmentAsync(contributionId, attachmentId);
        var caller = await GetCallerAsync();

        var contribution = await RequireContributionAsync(contributionId);

        if (contribution.SubmittedByUserId != caller.Id)
        {
            throw new ValidationException(
                "You can only remove files from contributions you submitted.");
        }

        // Row first: a deleted row with a stranded blob is recoverable housekeeping,
        // whereas a row pointing at a deleted blob is a broken download for everyone.
        await _attachmentRepository.RemoveAsync(attachment);

        await _fileStorage.DeleteAsync(
            FileStorageArea.ContributionAttachments,
            attachment.BlobName,
            cancellationToken);
    }

    private async Task<Contribution> RequireContributionAsync(int contributionId)
    {
        return await _contributionRepository.GetByIdAsync(contributionId)
            ?? throw new NotFoundException(
                $"Contribution {contributionId} does not exist.");
    }

    /// <summary>
    /// Loads the attachment and confirms it belongs to the contribution in the route, so
    /// one contribution's file cannot be read or deleted through another's URL.
    /// </summary>
    private async Task<ContributionAttachment> RequireAttachmentAsync(
        int contributionId,
        int attachmentId)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(attachmentId)
            ?? throw new NotFoundException($"Attachment {attachmentId} does not exist.");

        if (attachment.ContributionId != contributionId)
        {
            throw new NotFoundException(
                $"Attachment {attachmentId} does not belong to "
                + $"contribution {contributionId}.");
        }

        return attachment;
    }

    private async Task<User> GetCallerAsync()
    {
        var entraObjectId = _currentUserService.EntraObjectId;

        if (string.IsNullOrWhiteSpace(entraObjectId))
        {
            throw new UnauthorizedAccessException("Entra Object ID was not found.");
        }

        return await _userRepository.GetByEntraObjectIdAsync(entraObjectId)
            ?? throw new ValidationException(
                "Your profile has not been created yet. Reload the app and try again.");
    }

    private static string ValidateFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ValidationException("A file name is required.");
        }

        // Strip any path the client sent. Only the leaf is ever stored or echoed back.
        var leaf = Path.GetFileName(fileName.Trim());

        if (string.IsNullOrWhiteSpace(leaf))
        {
            throw new ValidationException("The file name is not valid.");
        }

        if (leaf.Length > ContributionAttachment.FileNameMaxLength)
        {
            throw new ValidationException(
                "File name cannot exceed "
                + $"{ContributionAttachment.FileNameMaxLength} characters.");
        }

        var extension = Path.GetExtension(leaf);

        if (BlockedExtensions.Contains(extension))
        {
            throw new ValidationException(
                $"Files of type {extension} cannot be attached.");
        }

        return leaf;
    }

    private static void ValidateSize(long length)
    {
        if (length <= 0)
        {
            throw new ValidationException("The file is empty.");
        }

        if (length > ContributionAttachment.MaxFileSizeBytes)
        {
            var megabytes = ContributionAttachment.MaxFileSizeBytes / (1024 * 1024);

            throw new ValidationException($"Files cannot exceed {megabytes} MB.");
        }
    }

    private static ContributionAttachmentDto MapToDto(ContributionAttachment attachment)
    {
        return new ContributionAttachmentDto
        {
            Id = attachment.Id,
            ContributionId = attachment.ContributionId,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            FileSize = attachment.FileSize,
            CreatedAt = attachment.CreatedAt
        };
    }
}
