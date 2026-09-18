using FluentAssertions;
using Moq;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Application.Interfaces.Services;
using TeamIntelligenceHub.Application.Services;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Tests.Services;

public class ContributionAttachmentServiceTests
{
    private readonly Mock<IContributionAttachmentRepository> _attachmentRepository = new();
    private readonly Mock<IContributionRepository> _contributionRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IFileStorage> _fileStorage = new();
    private readonly Mock<IDocumentInsightExtractionQueue> _extractionQueue = new();
    private readonly ContributionAttachmentService _sut;

    public ContributionAttachmentServiceTests()
    {
        _sut = new ContributionAttachmentService(
            _attachmentRepository.Object,
            _contributionRepository.Object,
            _userRepository.Object,
            _currentUserService.Object,
            _fileStorage.Object,
            _extractionQueue.Object);
    }

    private const string CallerEntraObjectId = "entra-object-id";

    private static Contribution CreateContribution(
        int id = 1,
        int submittedByUserId = 1)
    {
        return new Contribution
        {
            Id = id,
            InitiativeId = 1,
            SubmittedByUserId = submittedByUserId,
            Title = "Contribution Title",
            Description = "Contribution description that is long enough.",
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

    private static ContributionAttachment CreateAttachment(
        int id = 1,
        int contributionId = 1,
        string fileName = "evidence.pdf",
        string blobName = "blob-key",
        string contentType = "application/pdf",
        long fileSize = 100)
    {
        return new ContributionAttachment
        {
            Id = id,
            ContributionId = contributionId,
            FileName = fileName,
            BlobName = blobName,
            ContentType = contentType,
            FileSize = fileSize,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    private static FileUpload CreateUpload(
        string fileName = "evidence.pdf",
        string contentType = "application/pdf",
        long length = 100,
        Stream? content = null)
    {
        return new FileUpload(
            content ?? new MemoryStream(),
            fileName,
            contentType,
            length);
    }

    private void SetupCaller(User caller)
    {
        _currentUserService.Setup(x => x.EntraObjectId).Returns(caller.EntraObjectId);
        _userRepository
            .Setup(x => x.GetByEntraObjectIdAsync(caller.EntraObjectId))
            .ReturnsAsync(caller);
    }

    // ---------- UploadAsync ----------

    [Fact]
    public async Task UploadAsync_ContributionDoesNotExist_ThrowsNotFoundException()
    {
        _contributionRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Contribution?)null);

        var act = () => _sut.UploadAsync(1, CreateUpload());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UploadAsync_CallerIsNotSubmitter_ThrowsValidationException()
    {
        var contribution = CreateContribution(id: 1, submittedByUserId: 1);
        _contributionRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(contribution);

        var caller = CreateUser(id: 2);
        SetupCaller(caller);

        var act = () => _sut.UploadAsync(1, CreateUpload());

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UploadAsync_FileNameBlank_ThrowsValidationException(string? fileName)
    {
        var contribution = CreateContribution(id: 1, submittedByUserId: 1);
        _contributionRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(contribution);

        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        var act = () => _sut.UploadAsync(1, CreateUpload(fileName: fileName!));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UploadAsync_FileNameHasDirectoryPath_StripsToLeafOnly()
    {
        var contribution = CreateContribution(id: 1, submittedByUserId: 1);
        _contributionRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(contribution);

        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        _fileStorage
            .Setup(x => x.UploadAsync(
                FileStorageArea.ContributionAttachments,
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("stored-blob-name");

        _attachmentRepository
            .Setup(x => x.AddAsync(It.IsAny<ContributionAttachment>()))
            .ReturnsAsync((ContributionAttachment a) =>
            {
                a.Id = 1;
                return a;
            });

        var upload = CreateUpload(fileName: "C:\\evil\\path\\evidence.pdf");

        var result = await _sut.UploadAsync(1, upload);

        result.FileName.Should().Be("evidence.pdf");
        _fileStorage.Verify(
            x => x.UploadAsync(
                FileStorageArea.ContributionAttachments,
                It.IsAny<Stream>(),
                "evidence.pdf",
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UploadAsync_FileNameExceedsMaxLength_ThrowsValidationException()
    {
        var contribution = CreateContribution(id: 1, submittedByUserId: 1);
        _contributionRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(contribution);

        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        var longName = new string('a', ContributionAttachment.FileNameMaxLength + 1) + ".txt";

        var act = () => _sut.UploadAsync(1, CreateUpload(fileName: longName));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData("malware.exe")]
    [InlineData("script.ps1")]
    [InlineData("script.js")]
    public async Task UploadAsync_BlockedExtension_ThrowsValidationException(string fileName)
    {
        var contribution = CreateContribution(id: 1, submittedByUserId: 1);
        _contributionRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(contribution);

        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        var act = () => _sut.UploadAsync(1, CreateUpload(fileName: fileName));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UploadAsync_LengthIsZeroOrNegative_ThrowsValidationException()
    {
        var contribution = CreateContribution(id: 1, submittedByUserId: 1);
        _contributionRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(contribution);

        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        var act = () => _sut.UploadAsync(1, CreateUpload(length: 0));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UploadAsync_LengthExceedsMax_ThrowsValidationException()
    {
        var contribution = CreateContribution(id: 1, submittedByUserId: 1);
        _contributionRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(contribution);

        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        var act = () => _sut.UploadAsync(
            1,
            CreateUpload(length: ContributionAttachment.MaxFileSizeBytes + 1));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UploadAsync_ContentTypeBlank_DefaultsToOctetStream()
    {
        var contribution = CreateContribution(id: 1, submittedByUserId: 1);
        _contributionRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(contribution);

        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        _fileStorage
            .Setup(x => x.UploadAsync(
                FileStorageArea.ContributionAttachments,
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("stored-blob-name");

        _attachmentRepository
            .Setup(x => x.AddAsync(It.IsAny<ContributionAttachment>()))
            .ReturnsAsync((ContributionAttachment a) =>
            {
                a.Id = 1;
                return a;
            });

        var upload = CreateUpload(contentType: "   ");

        var result = await _sut.UploadAsync(1, upload);

        result.ContentType.Should().Be("application/octet-stream");
        _attachmentRepository.Verify(
            x => x.AddAsync(It.Is<ContributionAttachment>(
                a => a.ContentType == "application/octet-stream")),
            Times.Once);
    }

    [Fact]
    public async Task UploadAsync_ContentTypeExceedsMaxLength_TruncatedToMaxLength()
    {
        var contribution = CreateContribution(id: 1, submittedByUserId: 1);
        _contributionRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(contribution);

        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        _fileStorage
            .Setup(x => x.UploadAsync(
                FileStorageArea.ContributionAttachments,
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("stored-blob-name");

        _attachmentRepository
            .Setup(x => x.AddAsync(It.IsAny<ContributionAttachment>()))
            .ReturnsAsync((ContributionAttachment a) =>
            {
                a.Id = 1;
                return a;
            });

        var longContentType =
            new string('a', ContributionAttachment.ContentTypeMaxLength + 50);

        var result = await _sut.UploadAsync(1, CreateUpload(contentType: longContentType));

        result.ContentType.Should().HaveLength(ContributionAttachment.ContentTypeMaxLength);
    }

    [Fact]
    public async Task UploadAsync_ValidRequest_UploadsToContributionAttachmentsAreaWithPrefix()
    {
        var contribution = CreateContribution(id: 7, submittedByUserId: 1);
        _contributionRepository.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(contribution);

        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        _fileStorage
            .Setup(x => x.UploadAsync(
                FileStorageArea.ContributionAttachments,
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("stored-blob-name");

        _attachmentRepository
            .Setup(x => x.AddAsync(It.IsAny<ContributionAttachment>()))
            .ReturnsAsync((ContributionAttachment a) =>
            {
                a.Id = 1;
                return a;
            });

        await _sut.UploadAsync(7, CreateUpload());

        _fileStorage.Verify(
            x => x.UploadAsync(
                FileStorageArea.ContributionAttachments,
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                "contributions/7",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UploadAsync_ValidRequest_AddsAttachmentWithBlobNameFromStorageAndReturnsMappedDto()
    {
        var contribution = CreateContribution(id: 7, submittedByUserId: 1);
        _contributionRepository.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(contribution);

        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        _fileStorage
            .Setup(x => x.UploadAsync(
                FileStorageArea.ContributionAttachments,
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("stored-blob-name");

        _attachmentRepository
            .Setup(x => x.AddAsync(It.IsAny<ContributionAttachment>()))
            .ReturnsAsync((ContributionAttachment a) =>
            {
                a.Id = 55;
                return a;
            });

        var upload = CreateUpload(
            fileName: "evidence.pdf",
            contentType: "application/pdf",
            length: 12345);

        var result = await _sut.UploadAsync(7, upload);

        result.Id.Should().Be(55);
        result.ContributionId.Should().Be(7);
        result.FileName.Should().Be("evidence.pdf");
        result.ContentType.Should().Be("application/pdf");
        result.FileSize.Should().Be(12345);

        _attachmentRepository.Verify(
            x => x.AddAsync(It.Is<ContributionAttachment>(
                a => a.ContributionId == 7
                    && a.BlobName == "stored-blob-name"
                    && a.FileName == "evidence.pdf"
                    && a.ContentType == "application/pdf"
                    && a.FileSize == 12345)),
            Times.Once);
    }

    [Fact]
    public async Task UploadAsync_RepositoryAddThrows_DeletesUploadedBlobAndRethrows()
    {
        var contribution = CreateContribution(id: 1, submittedByUserId: 1);
        _contributionRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(contribution);

        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        _fileStorage
            .Setup(x => x.UploadAsync(
                FileStorageArea.ContributionAttachments,
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("stored-blob-name");

        _attachmentRepository
            .Setup(x => x.AddAsync(It.IsAny<ContributionAttachment>()))
            .ThrowsAsync(new InvalidOperationException("db failure"));

        var act = () => _sut.UploadAsync(1, CreateUpload());

        await act.Should().ThrowAsync<InvalidOperationException>();

        _fileStorage.Verify(
            x => x.DeleteAsync(
                FileStorageArea.ContributionAttachments,
                "stored-blob-name",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---------- DownloadAsync ----------

    [Fact]
    public async Task DownloadAsync_AttachmentDoesNotExist_ThrowsNotFoundException()
    {
        _attachmentRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync((ContributionAttachment?)null);

        var act = () => _sut.DownloadAsync(1, 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DownloadAsync_AttachmentBelongsToDifferentContribution_ThrowsNotFoundException()
    {
        var attachment = CreateAttachment(id: 1, contributionId: 2);
        _attachmentRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(attachment);

        var act = () => _sut.DownloadAsync(1, 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DownloadAsync_ValidRequest_ReturnsFileDownloadWithContentAndMetadata()
    {
        var attachment = CreateAttachment(
            id: 1,
            contributionId: 1,
            fileName: "evidence.pdf",
            blobName: "stored-blob-name",
            contentType: "application/pdf");
        _attachmentRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(attachment);

        using var contentStream = new MemoryStream([1, 2, 3]);
        _fileStorage
            .Setup(x => x.DownloadAsync(
                FileStorageArea.ContributionAttachments,
                "stored-blob-name",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentStream);

        var result = await _sut.DownloadAsync(1, 1);

        result.Content.Should().BeSameAs(contentStream);
        result.FileName.Should().Be("evidence.pdf");
        result.ContentType.Should().Be("application/pdf");
    }

    // ---------- RemoveAsync ----------

    [Fact]
    public async Task RemoveAsync_AttachmentDoesNotExist_ThrowsNotFoundException()
    {
        _attachmentRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync((ContributionAttachment?)null);

        var act = () => _sut.RemoveAsync(1, 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemoveAsync_AttachmentBelongsToDifferentContribution_ThrowsNotFoundException()
    {
        var attachment = CreateAttachment(id: 1, contributionId: 2);
        _attachmentRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(attachment);

        var act = () => _sut.RemoveAsync(1, 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemoveAsync_CallerIsNotSubmitter_ThrowsValidationException()
    {
        var attachment = CreateAttachment(id: 1, contributionId: 1);
        _attachmentRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(attachment);

        var caller = CreateUser(id: 2);
        SetupCaller(caller);

        var contribution = CreateContribution(id: 1, submittedByUserId: 1);
        _contributionRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(contribution);

        var act = () => _sut.RemoveAsync(1, 1);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task RemoveAsync_CallerIsSubmitter_RemovesRowBeforeDeletingBlob()
    {
        var attachment = CreateAttachment(
            id: 1,
            contributionId: 1,
            blobName: "stored-blob-name");
        _attachmentRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(attachment);

        var caller = CreateUser(id: 1);
        SetupCaller(caller);

        var contribution = CreateContribution(id: 1, submittedByUserId: 1);
        _contributionRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(contribution);

        var callOrder = new List<string>();
        _attachmentRepository
            .Setup(x => x.RemoveAsync(attachment))
            .Callback(() => callOrder.Add("RemoveRow"))
            .Returns(Task.CompletedTask);
        _fileStorage
            .Setup(x => x.DeleteAsync(
                FileStorageArea.ContributionAttachments,
                "stored-blob-name",
                It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("DeleteBlob"))
            .Returns(Task.CompletedTask);

        await _sut.RemoveAsync(1, 1);

        callOrder.Should().ContainInOrder("RemoveRow", "DeleteBlob");

        _attachmentRepository.Verify(x => x.RemoveAsync(attachment), Times.Once);
        _fileStorage.Verify(
            x => x.DeleteAsync(
                FileStorageArea.ContributionAttachments,
                "stored-blob-name",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
