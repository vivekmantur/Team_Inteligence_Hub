using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Application.Interfaces.Services;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Services;

public class TaskCommentService : ITaskCommentService
{
    private readonly ITaskCommentRepository _commentRepository;
    private readonly ITaskCommentAttachmentRepository _attachmentRepository;
    private readonly IInitiativeTaskRepository _taskRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;

    public TaskCommentService(
        ITaskCommentRepository commentRepository,
        ITaskCommentAttachmentRepository attachmentRepository,
        IInitiativeTaskRepository taskRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IFileStorage fileStorage)
    {
        _commentRepository = commentRepository;
        _attachmentRepository = attachmentRepository;
        _taskRepository = taskRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
    }

    public async Task<List<TaskCommentResponseDto>> GetByTaskAsync(int taskId)
    {
        await RequireTaskAsync(taskId);

        var comments = await _commentRepository.GetByTaskIdAsync(taskId);

        return comments
            .Select(MapToDto)
            .ToList();
    }

    public async Task<TaskCommentResponseDto> CreateAsync(
        int taskId,
        CreateTaskCommentRequestDto request)
    {
        await RequireTaskAsync(taskId);

        var caller = await GetCallerAsync();

        var parentCommentId = await ResolveParentAsync(taskId, request.ParentCommentId);
        var mentionedUserIds = await ResolveMentionsAsync(request.MentionedUserIds);

        var comment = new TaskComment
        {
            TaskId = taskId,
            // The author is whoever holds the token, never a value from the body.
            UserId = caller.Id,
            ParentCommentId = parentCommentId,
            CommentText = RequireText(request.CommentText),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        var created = await _commentRepository.AddAsync(comment);

        await _commentRepository.ReplaceMentionsAsync(created.Id, mentionedUserIds);

        return await ReloadAsync(created.Id);
    }

    public async Task<TaskCommentResponseDto> UpdateAsync(
        int taskId,
        int commentId,
        UpdateTaskCommentRequestDto request)
    {
        var comment = await RequireCommentAsync(taskId, commentId);
        var caller = await GetCallerAsync();

        if (comment.UserId != caller.Id)
        {
            throw new ValidationException("You can only edit your own comments.");
        }

        var mentionedUserIds = await ResolveMentionsAsync(request.MentionedUserIds);

        comment.CommentText = RequireText(request.CommentText);
        comment.UpdatedAt = DateTime.UtcNow;

        await _commentRepository.UpdateAsync(comment);

        await _commentRepository.ReplaceMentionsAsync(comment.Id, mentionedUserIds);

        return await ReloadAsync(comment.Id);
    }

    public async Task RemoveAsync(int taskId, int commentId)
    {
        var comment = await RequireCommentAsync(taskId, commentId);
        var caller = await GetCallerAsync();

        if (comment.UserId != caller.Id)
        {
            throw new ValidationException("You can only delete your own comments.");
        }

        // Collect the blob keys before the rows go — cascade clears the attachment
        // rows but never touches storage, which would strand the files.
        var blobNames = await _attachmentRepository
            .GetBlobNamesForCommentTreeAsync(comment.Id);

        // Replies go with it. The self-referencing key cannot cascade, so the
        // repository removes them explicitly.
        await _commentRepository.RemoveWithRepliesAsync(comment);

        foreach (var blobName in blobNames)
        {
            await _fileStorage.DeleteAsync(FileStorageArea.TaskAttachments, blobName);
        }
    }

    private async Task RequireTaskAsync(int taskId)
    {
        _ = await _taskRepository.GetByIdAsync(taskId)
            ?? throw new NotFoundException($"Task {taskId} does not exist.");
    }

    /// <summary>
    /// Loads a comment and confirms it belongs to the task in the route, so a comment on
    /// one task cannot be edited or deleted through another task's URL.
    /// </summary>
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

    /// <summary>
    /// Validates a reply target. Threads are one level deep: you reply to a comment, not
    /// to a reply, which keeps the discussion readable and the queries simple.
    /// </summary>
    private async Task<int?> ResolveParentAsync(int taskId, int? parentCommentId)
    {
        if (parentCommentId is null)
        {
            return null;
        }

        var parent = await _commentRepository.GetByIdAsync(parentCommentId.Value)
            ?? throw new ValidationException(
                $"Comment {parentCommentId} does not exist.");

        if (parent.TaskId != taskId)
        {
            throw new ValidationException(
                "You cannot reply to a comment on a different task.");
        }

        if (parent.ParentCommentId is not null)
        {
            throw new ValidationException(
                "Replies cannot be nested further. Reply to the original comment instead.");
        }

        return parent.Id;
    }

    /// <summary>
    /// Collapses duplicates and rejects unknown people, so a mention always resolves to
    /// somebody who can actually be notified.
    /// </summary>
    private async Task<List<int>> ResolveMentionsAsync(List<int>? mentionedUserIds)
    {
        if (mentionedUserIds is null || mentionedUserIds.Count == 0)
        {
            return [];
        }

        var distinct = mentionedUserIds.Distinct().ToList();

        foreach (var userId in distinct)
        {
            _ = await _userRepository.GetByIdAsync(userId)
                ?? throw new ValidationException(
                    $"Mentioned user {userId} does not exist.");
        }

        return distinct;
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

    private async Task<TaskCommentResponseDto> ReloadAsync(int commentId)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId)
            ?? throw new NotFoundException($"Comment {commentId} does not exist.");

        return MapToDto(comment);
    }

    private static string RequireText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ValidationException("Comment text is required.");
        }

        return text.Trim();
    }

    private static TaskCommentResponseDto MapToDto(TaskComment comment)
    {
        return new TaskCommentResponseDto
        {
            Id = comment.Id,
            TaskId = comment.TaskId,
            UserId = comment.UserId,
            UserDisplayName = comment.User?.DisplayName ?? string.Empty,
            ParentCommentId = comment.ParentCommentId,
            CommentText = comment.CommentText,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            Mentions = comment.Mentions
                .Select(m => new TaskCommentMentionDto
                {
                    MentionedUserId = m.MentionedUserId,
                    DisplayName = m.MentionedUser?.DisplayName ?? string.Empty
                })
                .ToList(),
            // Populated once attachments are wired; the column set already exists.
            Attachments = comment.Attachments
                .Select(a => new TaskCommentAttachmentDto
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    FileSize = a.FileSize,
                    CreatedAt = a.CreatedAt
                })
                .ToList()
        };
    }
}
