using FluentAssertions;
using Moq;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Application.Services;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Tests.Services;

public class TaskCommentServiceTests
{
    private readonly Mock<ITaskCommentRepository> _commentRepository = new();
    private readonly Mock<ITaskCommentAttachmentRepository> _attachmentRepository = new();
    private readonly Mock<IInitiativeTaskRepository> _taskRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IFileStorage> _fileStorage = new();
    private readonly TaskCommentService _sut;

    public TaskCommentServiceTests()
    {
        _sut = new TaskCommentService(
            _commentRepository.Object,
            _attachmentRepository.Object,
            _taskRepository.Object,
            _userRepository.Object,
            _currentUserService.Object,
            _fileStorage.Object);
    }

    private const string CallerEntraObjectId = "entra-object-id";

    private static InitiativeTask CreateTask(int id = 1)
    {
        return new InitiativeTask
        {
            Id = id,
            InitiativeId = 1,
            Title = "Task Title",
            CreatedByUserId = 1,
            Priority = default,
            Status = default,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    private static User CreateUser(
        int id = 1,
        string entraObjectId = CallerEntraObjectId,
        string displayName = "Caller Name")
    {
        return new User
        {
            Id = id,
            EntraObjectId = entraObjectId,
            Email = $"user{id}@example.com",
            DisplayName = displayName,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    private static TaskComment CreateComment(
        int id = 1,
        int taskId = 1,
        int userId = 1,
        int? parentCommentId = null,
        string commentText = "Some comment")
    {
        return new TaskComment
        {
            Id = id,
            TaskId = taskId,
            UserId = userId,
            ParentCommentId = parentCommentId,
            CommentText = commentText,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    private void SetupCaller(User caller)
    {
        _currentUserService.Setup(x => x.EntraObjectId).Returns(caller.EntraObjectId);
        _userRepository
            .Setup(x => x.GetByEntraObjectIdAsync(caller.EntraObjectId))
            .ReturnsAsync(caller);
    }

    // ---------- GetByTaskAsync ----------

    [Fact]
    public async Task GetByTaskAsync_TaskDoesNotExist_ThrowsNotFoundException()
    {
        _taskRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((InitiativeTask?)null);

        var act = () => _sut.GetByTaskAsync(1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByTaskAsync_TaskExists_ReturnsMappedComments()
    {
        _taskRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(CreateTask(1));
        var comments = new List<TaskComment>
        {
            CreateComment(id: 1, taskId: 1),
            CreateComment(id: 2, taskId: 1)
        };
        _commentRepository.Setup(x => x.GetByTaskIdAsync(1)).ReturnsAsync(comments);

        var result = await _sut.GetByTaskAsync(1);

        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().ContainInOrder(1, 2);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_TaskDoesNotExist_ThrowsNotFoundException()
    {
        _taskRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((InitiativeTask?)null);

        var request = new CreateTaskCommentRequestDto { CommentText = "Hello" };

        var act = () => _sut.CreateAsync(1, request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_CommentTextBlank_ThrowsValidationException(string? commentText)
    {
        _taskRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(CreateTask(1));
        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        var request = new CreateTaskCommentRequestDto { CommentText = commentText! };

        var act = () => _sut.CreateAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_UsesResolvedCallerAsAuthorNotRequestBody()
    {
        _taskRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(CreateTask(1));
        var caller = CreateUser(id: 42);
        SetupCaller(caller);

        TaskComment? addedComment = null;
        _commentRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskComment>()))
            .Callback<TaskComment>(c => addedComment = c)
            .ReturnsAsync((TaskComment c) =>
            {
                c.Id = 100;
                return c;
            });

        _commentRepository
            .Setup(x => x.GetByIdAsync(100))
            .ReturnsAsync(() => addedComment);

        var request = new CreateTaskCommentRequestDto
        {
            CommentText = "Hello world",
            // Even if a caller tried to smuggle a different author id in, there is no
            // UserId field on the request DTO at all — the author always comes from the
            // resolved caller, verified below.
        };

        var result = await _sut.CreateAsync(1, request);

        result.UserId.Should().Be(42);
        _commentRepository.Verify(
            x => x.AddAsync(It.Is<TaskComment>(c => c.UserId == 42)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_AddsCommentAndReplacesMentions()
    {
        _taskRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(CreateTask(1));
        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        _commentRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskComment>()))
            .ReturnsAsync((TaskComment c) =>
            {
                c.Id = 5;
                return c;
            });

        _commentRepository
            .Setup(x => x.GetByIdAsync(5))
            .ReturnsAsync(CreateComment(id: 5, taskId: 1, userId: 1));

        var request = new CreateTaskCommentRequestDto { CommentText = "Hello" };

        var result = await _sut.CreateAsync(1, request);

        result.Id.Should().Be(5);
        _commentRepository.Verify(
            x => x.ReplaceMentionsAsync(5, It.Is<List<int>>(m => m.Count == 0)),
            Times.Once);
    }

    // ---------- ResolveParentAsync (via CreateAsync) ----------

    [Fact]
    public async Task CreateAsync_ParentCommentIdIsNull_CreatesTopLevelComment()
    {
        _taskRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(CreateTask(1));
        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        _commentRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskComment>()))
            .ReturnsAsync((TaskComment c) =>
            {
                c.Id = 5;
                return c;
            });

        _commentRepository
            .Setup(x => x.GetByIdAsync(5))
            .ReturnsAsync(CreateComment(id: 5, taskId: 1, userId: 1));

        var request = new CreateTaskCommentRequestDto
        {
            CommentText = "Hello",
            ParentCommentId = null
        };

        var result = await _sut.CreateAsync(1, request);

        result.ParentCommentId.Should().BeNull();
        _commentRepository.Verify(
            x => x.AddAsync(It.Is<TaskComment>(c => c.ParentCommentId == null)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ParentCommentDoesNotExist_ThrowsValidationException()
    {
        _taskRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(CreateTask(1));
        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        _commentRepository.Setup(x => x.GetByIdAsync(99)).ReturnsAsync((TaskComment?)null);

        var request = new CreateTaskCommentRequestDto
        {
            CommentText = "Hello",
            ParentCommentId = 99
        };

        var act = () => _sut.CreateAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_ParentCommentBelongsToDifferentTask_ThrowsValidationException()
    {
        _taskRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(CreateTask(1));
        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        var parent = CreateComment(id: 10, taskId: 2, userId: 1);
        _commentRepository.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(parent);

        var request = new CreateTaskCommentRequestDto
        {
            CommentText = "Hello",
            ParentCommentId = 10
        };

        var act = () => _sut.CreateAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_ParentCommentIsAlreadyAReply_ThrowsValidationException()
    {
        _taskRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(CreateTask(1));
        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        var parent = CreateComment(id: 10, taskId: 1, userId: 1, parentCommentId: 3);
        _commentRepository.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(parent);

        var request = new CreateTaskCommentRequestDto
        {
            CommentText = "Hello",
            ParentCommentId = 10
        };

        var act = () => _sut.CreateAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_ParentCommentIsTopLevelOnSameTask_ResolvesAsReply()
    {
        _taskRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(CreateTask(1));
        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        var parent = CreateComment(id: 10, taskId: 1, userId: 1, parentCommentId: null);
        _commentRepository.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(parent);

        _commentRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskComment>()))
            .ReturnsAsync((TaskComment c) =>
            {
                c.Id = 20;
                return c;
            });

        _commentRepository
            .Setup(x => x.GetByIdAsync(20))
            .ReturnsAsync(CreateComment(id: 20, taskId: 1, userId: 1, parentCommentId: 10));

        var request = new CreateTaskCommentRequestDto
        {
            CommentText = "Reply text",
            ParentCommentId = 10
        };

        var result = await _sut.CreateAsync(1, request);

        result.ParentCommentId.Should().Be(10);
        _commentRepository.Verify(
            x => x.AddAsync(It.Is<TaskComment>(c => c.ParentCommentId == 10)),
            Times.Once);
    }

    // ---------- ResolveMentionsAsync (via CreateAsync) ----------

    [Fact]
    public async Task CreateAsync_MentionsNull_ReplacesMentionsWithEmptyList()
    {
        _taskRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(CreateTask(1));
        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        _commentRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskComment>()))
            .ReturnsAsync((TaskComment c) =>
            {
                c.Id = 5;
                return c;
            });
        _commentRepository
            .Setup(x => x.GetByIdAsync(5))
            .ReturnsAsync(CreateComment(id: 5, taskId: 1, userId: 1));

        var request = new CreateTaskCommentRequestDto
        {
            CommentText = "Hello",
            MentionedUserIds = null
        };

        await _sut.CreateAsync(1, request);

        _commentRepository.Verify(
            x => x.ReplaceMentionsAsync(5, It.Is<List<int>>(m => m.Count == 0)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_MentionsHaveDuplicates_DeduplicatedBeforeReplace()
    {
        _taskRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(CreateTask(1));
        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        _userRepository.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(CreateUser(id: 2));

        _commentRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskComment>()))
            .ReturnsAsync((TaskComment c) =>
            {
                c.Id = 5;
                return c;
            });
        _commentRepository
            .Setup(x => x.GetByIdAsync(5))
            .ReturnsAsync(CreateComment(id: 5, taskId: 1, userId: 1));

        var request = new CreateTaskCommentRequestDto
        {
            CommentText = "Hello",
            MentionedUserIds = [2, 2, 2]
        };

        await _sut.CreateAsync(1, request);

        _commentRepository.Verify(
            x => x.ReplaceMentionsAsync(
                5,
                It.Is<List<int>>(m => m.Count == 1 && m[0] == 2)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_MentionedUserDoesNotExist_ThrowsValidationException()
    {
        _taskRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(CreateTask(1));
        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        _userRepository.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((User?)null);

        var request = new CreateTaskCommentRequestDto
        {
            CommentText = "Hello",
            MentionedUserIds = [999]
        };

        var act = () => _sut.CreateAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_TaskDoesNotExist_ThrowsNotFoundException()
    {
        _commentRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((TaskComment?)null);

        var request = new UpdateTaskCommentRequestDto { CommentText = "Edited" };

        var act = () => _sut.UpdateAsync(1, 1, request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_CommentBelongsToDifferentTask_ThrowsNotFoundException()
    {
        var comment = CreateComment(id: 1, taskId: 2, userId: 1);
        _commentRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(comment);

        var request = new UpdateTaskCommentRequestDto { CommentText = "Edited" };

        var act = () => _sut.UpdateAsync(1, 1, request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_CallerIsNotAuthor_ThrowsValidationException()
    {
        var comment = CreateComment(id: 1, taskId: 1, userId: 1);
        _commentRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(comment);

        var caller = CreateUser(id: 2);
        SetupCaller(caller);

        var request = new UpdateTaskCommentRequestDto { CommentText = "Edited" };

        var act = () => _sut.UpdateAsync(1, 1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_CallerIsAuthor_UpdatesCommentTextAndMentions()
    {
        var comment = CreateComment(id: 1, taskId: 1, userId: 1, commentText: "Old text");
        _commentRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(comment);

        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        var request = new UpdateTaskCommentRequestDto
        {
            CommentText = "New text",
            MentionedUserIds = null
        };

        var result = await _sut.UpdateAsync(1, 1, request);

        comment.CommentText.Should().Be("New text");
        comment.UpdatedAt.Should().NotBeNull();
        result.CommentText.Should().Be("New text");

        _commentRepository.Verify(x => x.UpdateAsync(comment), Times.Once);
        _commentRepository.Verify(
            x => x.ReplaceMentionsAsync(1, It.Is<List<int>>(m => m.Count == 0)),
            Times.Once);
    }

    // ---------- RemoveAsync ----------

    [Fact]
    public async Task RemoveAsync_TaskDoesNotExist_ThrowsNotFoundException()
    {
        _commentRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((TaskComment?)null);

        var act = () => _sut.RemoveAsync(1, 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemoveAsync_CommentBelongsToDifferentTask_ThrowsNotFoundException()
    {
        var comment = CreateComment(id: 1, taskId: 2, userId: 1);
        _commentRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(comment);

        var act = () => _sut.RemoveAsync(1, 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemoveAsync_CallerIsNotAuthor_ThrowsValidationException()
    {
        var comment = CreateComment(id: 1, taskId: 1, userId: 1);
        _commentRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(comment);

        var caller = CreateUser(id: 2);
        SetupCaller(caller);

        var act = () => _sut.RemoveAsync(1, 1);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task RemoveAsync_CallerIsAuthor_CollectsBlobNamesBeforeRemovingThenDeletesEachBlob()
    {
        var comment = CreateComment(id: 1, taskId: 1, userId: 1);
        _commentRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(comment);

        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        var blobNames = new List<string> { "blob-one", "blob-two" };

        var callOrder = new List<string>();
        _attachmentRepository
            .Setup(x => x.GetBlobNamesForCommentTreeAsync(comment.Id))
            .Callback(() => callOrder.Add("GetBlobNames"))
            .ReturnsAsync(blobNames);
        _commentRepository
            .Setup(x => x.RemoveWithRepliesAsync(comment))
            .Callback(() => callOrder.Add("RemoveWithReplies"))
            .Returns(Task.CompletedTask);

        await _sut.RemoveAsync(1, 1);

        callOrder.Should().ContainInOrder("GetBlobNames", "RemoveWithReplies");

        _attachmentRepository.Verify(
            x => x.GetBlobNamesForCommentTreeAsync(comment.Id),
            Times.Once);
        _commentRepository.Verify(x => x.RemoveWithRepliesAsync(comment), Times.Once);

        _fileStorage.Verify(
            x => x.DeleteAsync(
                FileStorageArea.TaskAttachments,
                "blob-one",
                It.IsAny<CancellationToken>()),
            Times.Once);
        _fileStorage.Verify(
            x => x.DeleteAsync(
                FileStorageArea.TaskAttachments,
                "blob-two",
                It.IsAny<CancellationToken>()),
            Times.Once);
        _fileStorage.Verify(
            x => x.DeleteAsync(
                It.IsAny<FileStorageArea>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task RemoveAsync_NoAttachments_DoesNotCallFileStorageDelete()
    {
        var comment = CreateComment(id: 1, taskId: 1, userId: 1);
        _commentRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(comment);

        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        _attachmentRepository
            .Setup(x => x.GetBlobNamesForCommentTreeAsync(comment.Id))
            .ReturnsAsync([]);

        await _sut.RemoveAsync(1, 1);

        _fileStorage.Verify(
            x => x.DeleteAsync(
                It.IsAny<FileStorageArea>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
