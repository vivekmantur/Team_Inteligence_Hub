using FluentAssertions;
using Moq;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Application.Interfaces.Services;
using TeamIntelligenceHub.Application.Services;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Tests.Services;

public class TaskCommentAttachmentServiceTests
{
    private readonly Mock<ITaskCommentAttachmentRepository> _attachmentRepository = new();
    private readonly Mock<ITaskCommentRepository> _commentRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IFileStorage> _fileStorage = new();

    private readonly TaskCommentAttachmentService _sut;

    public TaskCommentAttachmentServiceTests()
    {
        _sut = new TaskCommentAttachmentService(
            _attachmentRepository.Object,
            _commentRepository.Object,
            _userRepository.Object,
            _currentUserService.Object,
            _fileStorage.Object);
    }

    private static User CreateUser(int id = 1, string entraObjectId = "entra-object-id")
    {
        return new User
        {
            Id = id,
            EntraObjectId = entraObjectId,
            Email = "user@example.com",
            DisplayName = "User Name",
            IsActive = true
        };
    }

    private static TaskComment CreateComment(int id = 1, int taskId = 1, int userId = 1)
    {
        return new TaskComment
        {
            Id = id,
            TaskId = taskId,
            UserId = userId,
            CommentText = "Some comment text",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static TaskCommentAttachment CreateAttachment(
        int id = 1,
        int taskCommentId = 1,
        TaskComment? taskComment = null,
        string fileName = "report.pdf",
        string blobName = "blob-key",
        string contentType = "application/pdf",
        long fileSize = 1024,
        DateTime? createdAt = null)
    {
        return new TaskCommentAttachment
        {
            Id = id,
            TaskCommentId = taskCommentId,
            FileName = fileName,
            BlobName = blobName,
            ContentType = contentType,
            FileSize = fileSize,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            TaskComment = taskComment!
        };
    }

    private void SetupCaller(User user)
    {
        _currentUserService.Setup(x => x.EntraObjectId).Returns(user.EntraObjectId);
        _userRepository
            .Setup(x => x.GetByEntraObjectIdAsync(user.EntraObjectId))
            .ReturnsAsync(user);
    }

    private static FileUpload CreateUpload(
        string fileName = "report.pdf",
        string contentType = "application/pdf",
        long length = 1024,
        Stream? content = null)
    {
        return new FileUpload(
            content ?? new MemoryStream(new byte[] { 1, 2, 3 }),
            fileName,
            contentType,
            length);
    }

    // ---------- UploadAsync: comment lookup ----------

    [Fact]
    public async Task UploadAsync_CommentDoesNotExist_ThrowsNotFoundException()
    {
        _commentRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync((TaskComment?)null);

        var act = () => _sut.UploadAsync(1, 5, CreateUpload());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UploadAsync_CommentBelongsToDifferentTask_ThrowsNotFoundException()
    {
        var comment = CreateComment(id: 5, taskId: 99);
        _commentRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(comment);

        var act = () => _sut.UploadAsync(1, 5, CreateUpload());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- UploadAsync: authorship ----------

    [Fact]
    public async Task UploadAsync_CallerIsNotCommentAuthor_ThrowsValidationException()
    {
        var comment = CreateComment(id: 5, taskId: 1, userId: 1);
        var caller = CreateUser(id: 2);

        _commentRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(comment);
        SetupCaller(caller);

        var act = () => _sut.UploadAsync(1, 5, CreateUpload());

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ---------- UploadAsync: file name validation ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UploadAsync_FileNameMissing_ThrowsValidationException(string? fileName)
    {
        var comment = CreateComment(id: 5, taskId: 1, userId: 1);
        var caller = CreateUser(id: 1);

        _commentRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(comment);
        SetupCaller(caller);

        var act = () => _sut.UploadAsync(1, 5, CreateUpload(fileName: fileName!));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData("../../evil/../secret.txt", "secret.txt")]
    [InlineData(@"C:\folder\file.txt", "file.txt")]
    public async Task UploadAsync_FileNameContainsPath_StripsToLeafNameOnly(
        string rawFileName, string expectedLeaf)
    {
        var comment = CreateComment(id: 5, taskId: 1, userId: 1);
        var caller = CreateUser(id: 1);

        _commentRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(comment);
        SetupCaller(caller);

        _fileStorage
            .Setup(x => x.UploadAsync(
                FileStorageArea.TaskAttachments,
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("stored-blob-key");

        _attachmentRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskCommentAttachment>()))
            .ReturnsAsync((TaskCommentAttachment a) => a);

        await _sut.UploadAsync(1, 5, CreateUpload(fileName: rawFileName));

        _fileStorage.Verify(x => x.UploadAsync(
            FileStorageArea.TaskAttachments,
            It.IsAny<Stream>(),
            expectedLeaf,
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _attachmentRepository.Verify(x => x.AddAsync(
            It.Is<TaskCommentAttachment>(a => a.FileName == expectedLeaf)), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_FileNameExceedsMaxLength_ThrowsValidationException()
    {
        var comment = CreateComment(id: 5, taskId: 1, userId: 1);
        var caller = CreateUser(id: 1);

        _commentRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(comment);
        SetupCaller(caller);

        var tooLongName = new string('a', TaskCommentAttachment.FileNameMaxLength + 1) + ".txt";

        var act = () => _sut.UploadAsync(1, 5, CreateUpload(fileName: tooLongName));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData("malware.exe")]
    [InlineData("script.ps1")]
    [InlineData("SCRIPT.JS")]
    public async Task UploadAsync_FileExtensionIsBlocked_ThrowsValidationException(string fileName)
    {
        var comment = CreateComment(id: 5, taskId: 1, userId: 1);
        var caller = CreateUser(id: 1);

        _commentRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(comment);
        SetupCaller(caller);

        var act = () => _sut.UploadAsync(1, 5, CreateUpload(fileName: fileName));

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ---------- UploadAsync: size validation ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UploadAsync_LengthNotPositive_ThrowsValidationException(long length)
    {
        var comment = CreateComment(id: 5, taskId: 1, userId: 1);
        var caller = CreateUser(id: 1);

        _commentRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(comment);
        SetupCaller(caller);

        var act = () => _sut.UploadAsync(1, 5, CreateUpload(length: length));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UploadAsync_LengthExceedsMax_ThrowsValidationException()
    {
        var comment = CreateComment(id: 5, taskId: 1, userId: 1);
        var caller = CreateUser(id: 1);

        _commentRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(comment);
        SetupCaller(caller);

        var act = () => _sut.UploadAsync(
            1, 5, CreateUpload(length: TaskCommentAttachment.MaxFileSizeBytes + 1));

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ---------- UploadAsync: content type handling ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UploadAsync_ContentTypeMissing_DefaultsToOctetStream(string? contentType)
    {
        var comment = CreateComment(id: 5, taskId: 1, userId: 1);
        var caller = CreateUser(id: 1);

        _commentRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(comment);
        SetupCaller(caller);

        _fileStorage
            .Setup(x => x.UploadAsync(
                FileStorageArea.TaskAttachments,
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("stored-blob-key");

        _attachmentRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskCommentAttachment>()))
            .ReturnsAsync((TaskCommentAttachment a) => a);

        var result = await _sut.UploadAsync(1, 5, CreateUpload(contentType: contentType!));

        result.ContentType.Should().Be("application/octet-stream");
    }

    [Fact]
    public async Task UploadAsync_ContentTypeExceedsMaxLength_TruncatedToMaxLength()
    {
        var comment = CreateComment(id: 5, taskId: 1, userId: 1);
        var caller = CreateUser(id: 1);

        _commentRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(comment);
        SetupCaller(caller);

        var tooLongContentType = new string('c', TaskCommentAttachment.ContentTypeMaxLength + 20);

        _fileStorage
            .Setup(x => x.UploadAsync(
                FileStorageArea.TaskAttachments,
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("stored-blob-key");

        _attachmentRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskCommentAttachment>()))
            .ReturnsAsync((TaskCommentAttachment a) => a);

        var result = await _sut.UploadAsync(1, 5, CreateUpload(contentType: tooLongContentType));

        result.ContentType.Should().HaveLength(TaskCommentAttachment.ContentTypeMaxLength);
        result.ContentType.Should().Be(tooLongContentType[..TaskCommentAttachment.ContentTypeMaxLength]);
    }

    // ---------- UploadAsync: successful path ----------

    [Fact]
    public async Task UploadAsync_Success_CallsFileStorageUploadWithAreaAndPrefix()
    {
        var comment = CreateComment(id: 5, taskId: 3, userId: 1);
        var caller = CreateUser(id: 1);

        _commentRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(comment);
        SetupCaller(caller);

        _fileStorage
            .Setup(x => x.UploadAsync(
                FileStorageArea.TaskAttachments,
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("stored-blob-key");

        _attachmentRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskCommentAttachment>()))
            .ReturnsAsync((TaskCommentAttachment a) => a);

        await _sut.UploadAsync(3, 5, CreateUpload());

        _fileStorage.Verify(x => x.UploadAsync(
            FileStorageArea.TaskAttachments,
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            "tasks/3/comments/5",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_Success_AddsAttachmentWithBlobNameReturnedByStorage()
    {
        var comment = CreateComment(id: 5, taskId: 3, userId: 1);
        var caller = CreateUser(id: 1);

        _commentRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(comment);
        SetupCaller(caller);

        _fileStorage
            .Setup(x => x.UploadAsync(
                FileStorageArea.TaskAttachments,
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("returned-blob-key");

        _attachmentRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskCommentAttachment>()))
            .ReturnsAsync((TaskCommentAttachment a) => a);

        await _sut.UploadAsync(3, 5, CreateUpload());

        _attachmentRepository.Verify(x => x.AddAsync(
            It.Is<TaskCommentAttachment>(a =>
                a.BlobName == "returned-blob-key" && a.TaskCommentId == 5)), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_Success_ReturnsMappedDto()
    {
        var comment = CreateComment(id: 5, taskId: 3, userId: 1);
        var caller = CreateUser(id: 1);

        _commentRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(comment);
        SetupCaller(caller);

        _fileStorage
            .Setup(x => x.UploadAsync(
                FileStorageArea.TaskAttachments,
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("returned-blob-key");

        _attachmentRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskCommentAttachment>()))
            .ReturnsAsync((TaskCommentAttachment a) =>
            {
                a.Id = 42;
                return a;
            });

        var result = await _sut.UploadAsync(
            3, 5, CreateUpload(fileName: "report.pdf", contentType: "application/pdf", length: 2048));

        result.Id.Should().Be(42);
        result.FileName.Should().Be("report.pdf");
        result.ContentType.Should().Be("application/pdf");
        result.FileSize.Should().Be(2048);
    }

    [Fact]
    public async Task UploadAsync_RepositoryAddAsyncThrows_DeletesBlobAndRethrows()
    {
        var comment = CreateComment(id: 5, taskId: 3, userId: 1);
        var caller = CreateUser(id: 1);

        _commentRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(comment);
        SetupCaller(caller);

        _fileStorage
            .Setup(x => x.UploadAsync(
                FileStorageArea.TaskAttachments,
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("orphaned-blob-key");

        var dbException = new InvalidOperationException("db is down");
        _attachmentRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskCommentAttachment>()))
            .ThrowsAsync(dbException);

        var act = () => _sut.UploadAsync(3, 5, CreateUpload());

        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Should().BeSameAs(dbException);

        _fileStorage.Verify(x => x.DeleteAsync(
            FileStorageArea.TaskAttachments,
            "orphaned-blob-key",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- DownloadAsync ----------

    [Fact]
    public async Task DownloadAsync_AttachmentDoesNotExist_ThrowsNotFoundException()
    {
        _attachmentRepository
            .Setup(x => x.GetByIdAsync(9))
            .ReturnsAsync((TaskCommentAttachment?)null);

        var act = () => _sut.DownloadAsync(1, 9);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DownloadAsync_AttachmentBelongsToDifferentTask_ThrowsNotFoundException()
    {
        var comment = CreateComment(id: 5, taskId: 99);
        var attachment = CreateAttachment(id: 9, taskCommentId: 5, taskComment: comment);

        _attachmentRepository.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(attachment);

        var act = () => _sut.DownloadAsync(1, 9);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DownloadAsync_Success_ReturnsFileDownloadFromStorage()
    {
        var comment = CreateComment(id: 5, taskId: 1);
        var attachment = CreateAttachment(
            id: 9,
            taskCommentId: 5,
            taskComment: comment,
            fileName: "report.pdf",
            blobName: "blob-key",
            contentType: "application/pdf");

        _attachmentRepository.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(attachment);

        using var contentStream = new MemoryStream(new byte[] { 9, 8, 7 });
        _fileStorage
            .Setup(x => x.DownloadAsync(
                FileStorageArea.TaskAttachments, "blob-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentStream);

        var result = await _sut.DownloadAsync(1, 9);

        result.Content.Should().BeSameAs(contentStream);
        result.FileName.Should().Be("report.pdf");
        result.ContentType.Should().Be("application/pdf");
    }

    // ---------- RemoveAsync ----------

    [Fact]
    public async Task RemoveAsync_AttachmentDoesNotExist_ThrowsNotFoundException()
    {
        _attachmentRepository
            .Setup(x => x.GetByIdAsync(9))
            .ReturnsAsync((TaskCommentAttachment?)null);

        var act = () => _sut.RemoveAsync(1, 9);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemoveAsync_AttachmentBelongsToDifferentTask_ThrowsNotFoundException()
    {
        var comment = CreateComment(id: 5, taskId: 99);
        var attachment = CreateAttachment(id: 9, taskCommentId: 5, taskComment: comment);

        _attachmentRepository.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(attachment);

        var act = () => _sut.RemoveAsync(1, 9);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemoveAsync_UnderlyingCommentNoLongerExists_ThrowsNotFoundException()
    {
        var comment = CreateComment(id: 5, taskId: 1, userId: 1);
        var attachment = CreateAttachment(id: 9, taskCommentId: 5, taskComment: comment);
        var caller = CreateUser(id: 1);

        _attachmentRepository.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(attachment);
        SetupCaller(caller);
        _commentRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync((TaskComment?)null);

        var act = () => _sut.RemoveAsync(1, 9);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemoveAsync_CallerIsNotCommentAuthor_ThrowsValidationException()
    {
        var comment = CreateComment(id: 5, taskId: 1, userId: 1);
        var attachment = CreateAttachment(id: 9, taskCommentId: 5, taskComment: comment);
        var caller = CreateUser(id: 2);

        _attachmentRepository.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(attachment);
        SetupCaller(caller);
        _commentRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(comment);

        var act = () => _sut.RemoveAsync(1, 9);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task RemoveAsync_Success_RemovesRowBeforeDeletingBlob()
    {
        var comment = CreateComment(id: 5, taskId: 1, userId: 1);
        var attachment = CreateAttachment(
            id: 9, taskCommentId: 5, taskComment: comment, blobName: "blob-key");
        var caller = CreateUser(id: 1);

        _attachmentRepository.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(attachment);
        SetupCaller(caller);
        _commentRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(comment);

        var callOrder = new List<string>();

        _attachmentRepository
            .Setup(x => x.RemoveAsync(attachment))
            .Callback(() => callOrder.Add("RemoveRow"))
            .Returns(Task.CompletedTask);

        _fileStorage
            .Setup(x => x.DeleteAsync(
                FileStorageArea.TaskAttachments, "blob-key", It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("DeleteBlob"))
            .Returns(Task.CompletedTask);

        await _sut.RemoveAsync(1, 9);

        _attachmentRepository.Verify(x => x.RemoveAsync(attachment), Times.Once);
        _fileStorage.Verify(x => x.DeleteAsync(
            FileStorageArea.TaskAttachments, "blob-key", It.IsAny<CancellationToken>()), Times.Once);

        callOrder.Should().ContainInOrder("RemoveRow", "DeleteBlob");
    }
}
