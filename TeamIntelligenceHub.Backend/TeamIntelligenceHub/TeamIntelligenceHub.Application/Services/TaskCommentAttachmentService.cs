using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Application.Interfaces.Services;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Services;

public class TaskCommentAttachmentService : ITaskCommentAttachmentService
{
    /// <summary>
    /// Extensions refused outright. A blocklist rather than an allowlist because people
    /// attach all sorts of legitimate things, but nobody needs to attach an executable.
    /// </summary>
    private static readonly HashSet<string> BlockedExtensions = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".bat", ".cmd", ".com", ".msi", ".scr",
        ".ps1", ".psm1", ".sh", ".vbs", ".vbe", ".js", ".jse",
        ".jar", ".reg", ".lnk", ".hta", ".cpl"
    };

    private readonly ITaskCommentAttachmentRepository _attachmentRepository;
    private readonly ITaskCommentRepository _commentRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;

    public TaskCommentAttachmentService(
        ITaskCommentAttachmentRepository attachmentRepository,
        ITaskCommentRepository commentRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IFileStorage fileStorage)
    {
        _attachmentRepository = attachmentRepository;
        _commentRepository = commentRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
    }

    public async Task<TaskCommentAttachmentDto> UploadAsync(
        int taskId,
        int commentId,
        FileUpload upload,
        CancellationToken cancellationToken = default)
    {
        var comment = await RequireCommentAsync(taskId, commentId);
        var caller = await GetCallerAsync();

        if (comment.UserId != caller.Id)
        {
            throw new ValidationException(
                "You can only attach files to your own comments.");
        }

        var fileName = ValidateFileName(upload.FileName);

        ValidateSize(upload.Length);

        var contentType = string.IsNullOrWhiteSpace(upload.ContentType)
            ? "application/octet-stream"
            : upload.ContentType.Trim();

        if (contentType.Length > TaskCommentAttachment.ContentTypeMaxLength)
        {
            contentType = contentType[..TaskCommentAttachment.ContentTypeMaxLength];
        }

        // Filed by entity so the container is browsable and everything belonging to a
        // task can be found, audited, or cleaned up as a unit.
        var blobName = await _fileStorage.UploadAsync(
            FileStorageArea.TaskAttachments,
            upload.Content,
            fileName,
            contentType,
            $"tasks/{taskId}/comments/{commentId}",
            cancellationToken);

        try
        {
            var attachment = await _attachmentRepository.AddAsync(
                new TaskCommentAttachment
                {
                    TaskCommentId = commentId,
                    FileName = fileName,
                    BlobName = blobName,
                    ContentType = contentType,
                    FileSize = upload.Length,
                    CreatedAt = DateTime.UtcNow
                });

            return MapToDto(attachment);
        }
        catch
        {
            // The bytes are already in storage. Without this the file would linger with
            // no row pointing at it, invisible and unbillable to anything.
            await _fileStorage.DeleteAsync(
                FileStorageArea.TaskAttachments, blobName, cancellationToken);
            throw;
        }
    }

    public async Task<FileDownload> DownloadAsync(
        int taskId,
        int attachmentId,
        CancellationToken cancellationToken = default)
    {
        var attachment = await RequireAttachmentAsync(taskId, attachmentId);

        var content = await _fileStorage.DownloadAsync(
            FileStorageArea.TaskAttachments, attachment.BlobName, cancellationToken);

        return new FileDownload(content, attachment.FileName, attachment.ContentType);
    }

    public async Task RemoveAsync(
        int taskId,
        int attachmentId,
        CancellationToken cancellationToken = default)
    {
        var attachment = await RequireAttachmentAsync(taskId, attachmentId);
        var caller = await GetCallerAsync();

        var comment = await _commentRepository.GetByIdAsync(attachment.TaskCommentId)
            ?? throw new NotFoundException("The comment no longer exists.");

        if (comment.UserId != caller.Id)
        {
            throw new ValidationException(
                "You can only remove files from your own comments.");
        }

        // Row first: a deleted row with a stranded blob is recoverable housekeeping,
        // whereas a row pointing at a deleted blob is a broken download for everyone.
        await _attachmentRepository.RemoveAsync(attachment);

        await _fileStorage.DeleteAsync(
            FileStorageArea.TaskAttachments, attachment.BlobName, cancellationToken);
    }

    private async Task<TaskComment> RequireCommentAsync(int taskId, int commentId)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId)
            ?? throw new NotFoundException($"Comment {commentId} does not exist.");

        if (comment.TaskId != taskId)
        {
            throw new NotFoundException(
                $"Comment {commentId} does not belong to task {taskId}.");
        }

        return comment;
    }

    private async Task<TaskCommentAttachment> RequireAttachmentAsync(
        int taskId,
        int attachmentId)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(attachmentId)
            ?? throw new NotFoundException($"Attachment {attachmentId} does not exist.");

        if (attachment.TaskComment?.TaskId != taskId)
        {
            throw new NotFoundException(
                $"Attachment {attachmentId} does not belong to task {taskId}.");
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

        if (leaf.Length > TaskCommentAttachment.FileNameMaxLength)
        {
            throw new ValidationException(
                $"File name cannot exceed {TaskCommentAttachment.FileNameMaxLength} characters.");
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

        if (length > TaskCommentAttachment.MaxFileSizeBytes)
        {
            var limitMb = TaskCommentAttachment.MaxFileSizeBytes / (1024 * 1024);

            throw new ValidationException($"Files cannot exceed {limitMb} MB.");
        }
    }

    private static TaskCommentAttachmentDto MapToDto(TaskCommentAttachment attachment)
    {
        return new TaskCommentAttachmentDto
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            FileSize = attachment.FileSize,
            CreatedAt = attachment.CreatedAt
        };
    }
}
