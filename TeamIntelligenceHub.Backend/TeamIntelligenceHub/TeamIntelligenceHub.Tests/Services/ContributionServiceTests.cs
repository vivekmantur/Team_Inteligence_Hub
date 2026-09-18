using FluentAssertions;
using Moq;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Application.Services;
using TeamIntelligenceHub.Domain.Entities;
using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Tests.Services;

public class ContributionServiceTests
{
    private readonly Mock<IContributionRepository> _contributionRepository = new();
    private readonly Mock<IInitiativeRepository> _initiativeRepository = new();
    private readonly Mock<IInitiativeMemberRepository> _memberRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IFileStorage> _fileStorage = new();
    private readonly Mock<IDocumentTestimonialAndCustomerStoryRepository> _documentInsightRepository = new();

    private readonly ContributionService _sut;

    public ContributionServiceTests()
    {
        _sut = new ContributionService(
            _contributionRepository.Object,
            _initiativeRepository.Object,
            _memberRepository.Object,
            _userRepository.Object,
            _currentUserService.Object,
            _fileStorage.Object,
            _documentInsightRepository.Object);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Initiative CreateInitiative(int id = 1)
    {
        return new Initiative { Id = id };
    }

    private static User CreateUser(int id = 1, bool isActive = true, string? displayName = null)
    {
        return new User
        {
            Id = id,
            DisplayName = displayName ?? $"User {id}",
            Email = $"user{id}@example.com",
            IsActive = isActive,
            EntraObjectId = $"entra-{id}"
        };
    }

    private static Contribution CreateContribution(
        int id = 10,
        int initiativeId = 1,
        int submittedByUserId = 1,
        ContributionStatus status = ContributionStatus.Draft,
        DateTime? submittedAt = null,
        List<ContributionAttachment>? attachments = null)
    {
        return new Contribution
        {
            Id = id,
            InitiativeId = initiativeId,
            SubmittedByUserId = submittedByUserId,
            Title = "Existing title",
            Description = "Existing description",
            Status = status,
            SubmittedAt = submittedAt,
            Types = [ContributionType.ProgressUpdate],
            Attachments = attachments ?? []
        };
    }

    private static CreateContributionRequestDto BuildValidRequest(
        List<ContributionType>? types = null,
        ContributionStatus? status = null,
        List<string>? tags = null,
        List<ContributionContributorRequestDto>? contributors = null,
        List<ContributionLinkRequestDto>? links = null,
        ContributionMetricRequestDto? metric = null,
        ContributionRiskRequestDto? risk = null,
        ContributionAiPracticeRequestDto? aiPractice = null,
        ContributionCustomerStoryRequestDto? customerStory = null,
        ContributionTestimonialRequestDto? testimonial = null)
    {
        return new CreateContributionRequestDto
        {
            Title = "A valid title",
            Description = "A valid description that is long enough.",
            Types = types ?? [ContributionType.ProgressUpdate],
            Status = status,
            Tags = tags,
            Contributors = contributors,
            Links = links,
            Metric = metric,
            Risk = risk,
            AiPractice = aiPractice,
            CustomerStory = customerStory,
            Testimonial = testimonial
        };
    }

    private void SetupInitiative(int id = 1, Initiative? initiative = null)
    {
        _initiativeRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(initiative ?? CreateInitiative(id));
    }

    private void SetupCaller(User caller)
    {
        _currentUserService.Setup(c => c.EntraObjectId).Returns(caller.EntraObjectId);
        _userRepository.Setup(r => r.GetByEntraObjectIdAsync(caller.EntraObjectId)).ReturnsAsync(caller);
    }

    /// <summary>
    /// Wires AddAsync to assign an Id and hand back the same instance, and wires the
    /// follow-up GetByIdAsync (used by ReloadAsync) so the create pipeline can run to
    /// completion. Returns a one-element holder exposing the contribution that was
    /// actually passed to AddAsync, for asserting what the service built.
    /// </summary>
    private Contribution?[] SetupCreatePipeline(int assignedId = 100)
    {
        var holder = new Contribution?[1];

        _contributionRepository
            .Setup(r => r.AddAsync(It.IsAny<Contribution>()))
            .Callback<Contribution>(c => holder[0] = c)
            .ReturnsAsync((Contribution c) =>
            {
                c.Id = assignedId;
                return c;
            });

        _contributionRepository
            .Setup(r => r.GetByIdAsync(assignedId))
            .ReturnsAsync(new Contribution { Id = assignedId });

        return holder;
    }

    private List<ContributionContributor> CaptureContributors()
    {
        var captured = new List<ContributionContributor>();

        _contributionRepository
            .Setup(r => r.ReplaceContributorsAsync(
                It.IsAny<int>(), It.IsAny<IReadOnlyCollection<ContributionContributor>>()))
            .Callback<int, IReadOnlyCollection<ContributionContributor>>(
                (_, contributors) => captured.AddRange(contributors))
            .Returns(Task.CompletedTask);

        return captured;
    }

    private List<ContributionLink> CaptureLinks()
    {
        var captured = new List<ContributionLink>();

        _contributionRepository
            .Setup(r => r.ReplaceLinksAsync(
                It.IsAny<int>(), It.IsAny<IReadOnlyCollection<ContributionLink>>()))
            .Callback<int, IReadOnlyCollection<ContributionLink>>(
                (_, links) => captured.AddRange(links))
            .Returns(Task.CompletedTask);

        return captured;
    }

    private ContributionTestimonial?[] CaptureTestimonial()
    {
        var holder = new ContributionTestimonial?[1];

        _contributionRepository
            .Setup(r => r.SaveDetailSectionsAsync(
                It.IsAny<int>(),
                It.IsAny<ContributionMetric?>(),
                It.IsAny<ContributionRisk?>(),
                It.IsAny<ContributionAiPractice?>(),
                It.IsAny<ContributionCustomerStory?>(),
                It.IsAny<ContributionTestimonial?>()))
            .Callback<
                int, ContributionMetric?, ContributionRisk?, ContributionAiPractice?,
                ContributionCustomerStory?, ContributionTestimonial?>(
                (_, _, _, _, _, testimonial) => holder[0] = testimonial)
            .Returns(Task.CompletedTask);

        return holder;
    }

    // -----------------------------------------------------------------------
    // NormaliseTypes
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_TypesEmpty_ThrowsValidationException()
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        SetupCreatePipeline();

        var request = BuildValidRequest(types: []);

        var act = () => _sut.CreateAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_DuplicateTypes_DeduplicatesBeforeStoring()
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        var added = SetupCreatePipeline();

        var request = BuildValidRequest(
            types: [ContributionType.ProgressUpdate, ContributionType.ProgressUpdate, ContributionType.Deliverable]);

        await _sut.CreateAsync(1, request);

        added[0]!.Types.Should().BeEquivalentTo(
            [ContributionType.ProgressUpdate, ContributionType.Deliverable]);
    }

    // -----------------------------------------------------------------------
    // RequireSectionsMatchTypes
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_MetricWithoutBusinessMetricType_ThrowsValidationException()
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        SetupCreatePipeline();

        var request = BuildValidRequest(
            types: [ContributionType.ProgressUpdate],
            metric: new ContributionMetricRequestDto { MetricName = "Adoption" });

        var act = () => _sut.CreateAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_RiskWithoutRiskType_ThrowsValidationException()
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        SetupCreatePipeline();

        var request = BuildValidRequest(
            types: [ContributionType.ProgressUpdate],
            risk: new ContributionRiskRequestDto { Description = "Something might break." });

        var act = () => _sut.CreateAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_AiPracticeWithoutAiBestPracticeType_ThrowsValidationException()
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        SetupCreatePipeline();

        var request = BuildValidRequest(
            types: [ContributionType.ProgressUpdate],
            aiPractice: new ContributionAiPracticeRequestDto { Tool = "Copilot" });

        var act = () => _sut.CreateAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_CustomerStoryWithoutCustomerStoryType_ThrowsValidationException()
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        SetupCreatePipeline();

        var request = BuildValidRequest(
            types: [ContributionType.ProgressUpdate],
            customerStory: new ContributionCustomerStoryRequestDto { CustomerName = "Acme" });

        var act = () => _sut.CreateAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_TestimonialWithoutTestimonialType_ThrowsValidationException()
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        SetupCreatePipeline();

        var request = BuildValidRequest(
            types: [ContributionType.ProgressUpdate],
            testimonial: new ContributionTestimonialRequestDto
            {
                Quote = "Great work.",
                SpeakerName = "Anita Rao"
            });

        var act = () => _sut.CreateAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // -----------------------------------------------------------------------
    // NormaliseTags
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_TagsDuplicateCaseInsensitive_KeepsFirstSpelling()
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        var added = SetupCreatePipeline();

        var request = BuildValidRequest(tags: ["Copilot", "copilot"]);

        await _sut.CreateAsync(1, request);

        added[0]!.Tags.Should().Equal("Copilot");
    }

    [Fact]
    public async Task CreateAsync_TagsBlankOrWhitespace_AreDropped()
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        var added = SetupCreatePipeline();

        var request = BuildValidRequest(tags: [" ", "", "Valid"]);

        await _sut.CreateAsync(1, request);

        added[0]!.Tags.Should().Equal("Valid");
    }

    [Fact]
    public async Task CreateAsync_TagsWithSurroundingWhitespace_AreTrimmed()
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        var added = SetupCreatePipeline();

        var request = BuildValidRequest(tags: ["  Spacey  "]);

        await _sut.CreateAsync(1, request);

        added[0]!.Tags.Should().Equal("Spacey");
    }

    [Fact]
    public async Task CreateAsync_TagExceedsMaxLength_ThrowsValidationException()
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        SetupCreatePipeline();

        var request = BuildValidRequest(tags: [new string('a', Contribution.TagMaxLength + 1)]);

        var act = () => _sut.CreateAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // -----------------------------------------------------------------------
    // RequireUrl (via links)
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/relative/path")]
    [InlineData("javascript:alert(1)")]
    public async Task CreateAsync_LinkUrlInvalid_ThrowsValidationException(string? url)
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        SetupCreatePipeline();

        var request = BuildValidRequest(links:
        [
            new ContributionLinkRequestDto { Source = ContributionLinkSource.ExternalUrl, Url = url! }
        ]);

        var act = () => _sut.CreateAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_LinkUrlExceedsMaxLength_ThrowsValidationException()
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        SetupCreatePipeline();

        var longUrl = "https://example.com/" + new string('a', ContributionLink.UrlMaxLength);

        var request = BuildValidRequest(links:
        [
            new ContributionLinkRequestDto { Source = ContributionLinkSource.ExternalUrl, Url = longUrl }
        ]);

        var act = () => _sut.CreateAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_LinkUrlValidHttpsUrl_IsStored()
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        SetupCreatePipeline();
        var capturedLinks = CaptureLinks();

        var request = BuildValidRequest(links:
        [
            new ContributionLinkRequestDto
            {
                Source = ContributionLinkSource.SharePoint,
                Url = "https://example.com/doc",
                Label = "Doc"
            }
        ]);

        await _sut.CreateAsync(1, request);

        capturedLinks.Should().ContainSingle();
        capturedLinks[0].Url.Should().Be("https://example.com/doc");
        capturedLinks[0].Source.Should().Be(ContributionLinkSource.SharePoint);
        capturedLinks[0].Label.Should().Be("Doc");
    }

    // -----------------------------------------------------------------------
    // BuildContributorsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ContributorDoesNotExist_ThrowsValidationException()
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        SetupCreatePipeline();
        _userRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        var request = BuildValidRequest(contributors:
        [
            new ContributionContributorRequestDto { UserId = 99 }
        ]);

        var act = () => _sut.CreateAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_ContributorIsDeactivated_ThrowsValidationException()
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        SetupCreatePipeline();
        _userRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(CreateUser(99, isActive: false));

        var request = BuildValidRequest(contributors:
        [
            new ContributionContributorRequestDto { UserId = 99 }
        ]);

        var act = () => _sut.CreateAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_CallerNotInContributorsList_AddsCallerAsPrimary()
    {
        var caller = CreateUser(1);
        SetupInitiative();
        SetupCaller(caller);
        SetupCreatePipeline();
        var captured = CaptureContributors();
        _userRepository.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(CreateUser(2));

        var request = BuildValidRequest(contributors:
        [
            new ContributionContributorRequestDto { UserId = 2, IsPrimary = false }
        ]);

        await _sut.CreateAsync(1, request);

        captured.Should().HaveCount(2);
        captured.Should().ContainSingle(c => c.UserId == 1 && c.IsPrimary);
        captured.Should().ContainSingle(c => c.UserId == 2 && !c.IsPrimary);
    }

    [Fact]
    public async Task CreateAsync_DuplicateContributorUserId_LastMentionWins()
    {
        var caller = CreateUser(1);
        SetupInitiative();
        SetupCaller(caller);
        SetupCreatePipeline();
        var captured = CaptureContributors();
        _userRepository.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(CreateUser(2));

        var request = BuildValidRequest(contributors:
        [
            new ContributionContributorRequestDto { UserId = 2, ResponsibilityArea = "First", IsPrimary = false },
            new ContributionContributorRequestDto { UserId = 2, ResponsibilityArea = "Second", IsPrimary = true }
        ]);

        await _sut.CreateAsync(1, request);

        var forUser2 = captured.Where(c => c.UserId == 2).ToList();
        forUser2.Should().ContainSingle();
        forUser2[0].ResponsibilityArea.Should().Be("Second");
        forUser2[0].IsPrimary.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // CreateAsync general
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_InitiativeDoesNotExist_ThrowsNotFoundException()
    {
        _initiativeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Initiative?)null);

        var request = BuildValidRequest();

        var act = () => _sut.CreateAsync(1, request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_SubmittedByUserId_ComesFromCallerNotRequest()
    {
        var caller = CreateUser(7);
        SetupInitiative();
        SetupCaller(caller);
        var added = SetupCreatePipeline();

        var request = BuildValidRequest();

        await _sut.CreateAsync(1, request);

        added[0]!.SubmittedByUserId.Should().Be(7);
    }

    [Fact]
    public async Task CreateAsync_StatusSubmitted_SetsSubmittedAtToNow()
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        var added = SetupCreatePipeline();

        var before = DateTime.UtcNow;
        var request = BuildValidRequest(status: ContributionStatus.Submitted);

        await _sut.CreateAsync(1, request);

        var after = DateTime.UtcNow;
        added[0]!.SubmittedAt.Should().NotBeNull();
        added[0]!.SubmittedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task CreateAsync_StatusOmittedDefaultsToDraft_SubmittedAtIsNull()
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        var added = SetupCreatePipeline();

        var request = BuildValidRequest(status: null);

        await _sut.CreateAsync(1, request);

        added[0]!.Status.Should().Be(ContributionStatus.Draft);
        added[0]!.SubmittedAt.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // UpdateAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_CallerIsNotSubmitter_ThrowsValidationException()
    {
        var contribution = CreateContribution(id: 10, submittedByUserId: 1);
        _contributionRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(contribution);
        SetupCaller(CreateUser(2));

        var request = new UpdateContributionRequestDto
        {
            Title = "New title",
            Description = "New description that is long enough.",
            Types = [ContributionType.ProgressUpdate]
        };

        var act = () => _sut.UpdateAsync(10, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_ContributionDoesNotExist_ThrowsNotFoundException()
    {
        _contributionRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((Contribution?)null);

        var request = new UpdateContributionRequestDto
        {
            Title = "New title",
            Description = "New description that is long enough.",
            Types = [ContributionType.ProgressUpdate]
        };

        var act = () => _sut.UpdateAsync(10, request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_DraftToSubmitted_StampsSubmittedAt()
    {
        var contribution = CreateContribution(
            id: 10, initiativeId: 1, submittedByUserId: 1, status: ContributionStatus.Draft, submittedAt: null);
        _contributionRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(contribution);
        SetupCaller(CreateUser(1));

        var before = DateTime.UtcNow;

        var request = new UpdateContributionRequestDto
        {
            Title = "New title",
            Description = "New description that is long enough.",
            Types = [ContributionType.ProgressUpdate],
            Status = ContributionStatus.Submitted
        };

        await _sut.UpdateAsync(10, request);

        var after = DateTime.UtcNow;
        contribution.Status.Should().Be(ContributionStatus.Submitted);
        contribution.SubmittedAt.Should().NotBeNull();
        contribution.SubmittedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task UpdateAsync_AlreadySubmitted_DoesNotChangeSubmittedAt()
    {
        var fixedSubmittedAt = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var contribution = CreateContribution(
            id: 10,
            initiativeId: 1,
            submittedByUserId: 1,
            status: ContributionStatus.Submitted,
            submittedAt: fixedSubmittedAt);
        _contributionRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(contribution);
        SetupCaller(CreateUser(1));

        var request = new UpdateContributionRequestDto
        {
            Title = "Edited title",
            Description = "Edited description that is long enough.",
            Types = [ContributionType.ProgressUpdate],
            Status = ContributionStatus.Submitted
        };

        await _sut.UpdateAsync(10, request);

        contribution.SubmittedAt.Should().Be(fixedSubmittedAt);
    }

    // -----------------------------------------------------------------------
    // BuildAiPractice
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_AiPracticeTimeSavedNegative_ThrowsValidationException()
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        SetupCreatePipeline();

        var request = BuildValidRequest(
            types: [ContributionType.AiBestPractice],
            aiPractice: new ContributionAiPracticeRequestDto { Tool = "Copilot", TimeSavedHoursPerWeek = -1 });

        var act = () => _sut.CreateAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_AiPracticeTimeSavedExceedsMax_ThrowsValidationException()
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        SetupCreatePipeline();

        var request = BuildValidRequest(
            types: [ContributionType.AiBestPractice],
            aiPractice: new ContributionAiPracticeRequestDto
            {
                Tool = "Copilot",
                TimeSavedHoursPerWeek = ContributionAiPractice.MaxTimeSavedHoursPerWeek + 1
            });

        var act = () => _sut.CreateAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // -----------------------------------------------------------------------
    // EnsureTeamMembershipAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ContributorNotAlreadyMember_AddsToInitiativeTeam()
    {
        SetupInitiative(1);
        SetupCaller(CreateUser(1));
        SetupCreatePipeline();
        _memberRepository.Setup(m => m.ExistsAsync(1, 1)).ReturnsAsync(false);

        var request = BuildValidRequest();

        await _sut.CreateAsync(1, request);

        _memberRepository.Verify(
            m => m.AddAsync(It.Is<InitiativeMember>(
                im => im.InitiativeId == 1 && im.UserId == 1 && im.Role == "Contributor")),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ContributorAlreadyMember_DoesNotAddDuplicateMembership()
    {
        SetupInitiative(1);
        SetupCaller(CreateUser(1));
        SetupCreatePipeline();
        _memberRepository.Setup(m => m.ExistsAsync(1, 1)).ReturnsAsync(true);

        var request = BuildValidRequest();

        await _sut.CreateAsync(1, request);

        _memberRepository.Verify(m => m.AddAsync(It.IsAny<InitiativeMember>()), Times.Never);
    }

    // -----------------------------------------------------------------------
    // RemoveAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RemoveAsync_CallerIsNotSubmitter_ThrowsValidationException()
    {
        var contribution = CreateContribution(id: 10, submittedByUserId: 1);
        _contributionRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(contribution);
        SetupCaller(CreateUser(2));

        var act = () => _sut.RemoveAsync(10);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task RemoveAsync_ContributionDoesNotExist_ThrowsNotFoundException()
    {
        _contributionRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((Contribution?)null);

        var act = () => _sut.RemoveAsync(10);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemoveAsync_DeletesAttachmentBlobsBeforeRemovingContribution()
    {
        var attachments = new List<ContributionAttachment>
        {
            new() { Id = 1, ContributionId = 10, FileName = "f1.txt", BlobName = "blob-1", ContentType = "text/plain" },
            new() { Id = 2, ContributionId = 10, FileName = "f2.txt", BlobName = "blob-2", ContentType = "text/plain" }
        };
        var contribution = CreateContribution(id: 10, submittedByUserId: 1, attachments: attachments);
        _contributionRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(contribution);
        SetupCaller(CreateUser(1));

        var callOrder = new List<string>();

        _fileStorage
            .Setup(f => f.DeleteAsync(
                FileStorageArea.ContributionAttachments, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<FileStorageArea, string, CancellationToken>((_, blobName, _) => callOrder.Add($"delete:{blobName}"))
            .Returns(Task.CompletedTask);

        _contributionRepository
            .Setup(r => r.RemoveAsync(contribution))
            .Callback(() => callOrder.Add("remove"))
            .Returns(Task.CompletedTask);

        await _sut.RemoveAsync(10);

        callOrder.Should().Equal("delete:blob-1", "delete:blob-2", "remove");
        _fileStorage.Verify(
            f => f.DeleteAsync(FileStorageArea.ContributionAttachments, "blob-1", It.IsAny<CancellationToken>()),
            Times.Once);
        _fileStorage.Verify(
            f => f.DeleteAsync(FileStorageArea.ContributionAttachments, "blob-2", It.IsAny<CancellationToken>()),
            Times.Once);
        _contributionRepository.Verify(r => r.RemoveAsync(contribution), Times.Once);
    }

    // -----------------------------------------------------------------------
    // GetTagVocabularyAsync
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(null, 20)]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(50, 50)]
    [InlineData(500, 100)]
    public async Task GetTagVocabularyAsync_Take_IsClampedToValidRange(int? take, int expected)
    {
        _contributionRepository
            .Setup(r => r.GetTagVocabularyAsync(It.IsAny<string?>(), It.IsAny<int>()))
            .ReturnsAsync([]);

        await _sut.GetTagVocabularyAsync(null, take);

        _contributionRepository.Verify(r => r.GetTagVocabularyAsync(null, expected), Times.Once);
    }

    // -----------------------------------------------------------------------
    // BuildTestimonial
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_TestimonialWithoutAudienceOrSentiment_DefaultsToStakeholderAndPositive()
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        SetupCreatePipeline();
        var captured = CaptureTestimonial();

        var request = BuildValidRequest(
            types: [ContributionType.Testimonial],
            testimonial: new ContributionTestimonialRequestDto
            {
                Quote = "This team is the storytelling engine of Modern Work.",
                SpeakerName = "Anita Rao"
            });

        await _sut.CreateAsync(1, request);

        captured[0].Should().NotBeNull();
        captured[0]!.Audience.Should().Be(TestimonialAudience.Stakeholder);
        captured[0]!.Sentiment.Should().Be(TestimonialSentiment.Positive);
    }

    [Fact]
    public async Task CreateAsync_TestimonialWithExplicitAudienceAndSentiment_StoresThem()
    {
        SetupInitiative();
        SetupCaller(CreateUser());
        SetupCreatePipeline();
        var captured = CaptureTestimonial();

        var request = BuildValidRequest(
            types: [ContributionType.Testimonial],
            testimonial: new ContributionTestimonialRequestDto
            {
                Quote = "Would love more granular agent telemetry in Fabric.",
                SpeakerName = "Reece Patterson",
                SpeakerRole = "Analyst",
                Audience = TestimonialAudience.Customer,
                Sentiment = TestimonialSentiment.Constructive
            });

        await _sut.CreateAsync(1, request);

        captured[0].Should().NotBeNull();
        captured[0]!.Quote.Should().Be("Would love more granular agent telemetry in Fabric.");
        captured[0]!.SpeakerName.Should().Be("Reece Patterson");
        captured[0]!.SpeakerRole.Should().Be("Analyst");
        captured[0]!.Audience.Should().Be(TestimonialAudience.Customer);
        captured[0]!.Sentiment.Should().Be(TestimonialSentiment.Constructive);
    }

    // -----------------------------------------------------------------------
    // GetTestimonialsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetTestimonialsAsync_MapsContributionsToTestimonialCards()
    {
        var contribution = CreateContribution(id: 5, status: ContributionStatus.Submitted);
        contribution.Initiative = new Initiative { Id = 1, Name = "Role Hub Rollout" };
        contribution.Testimonial = new ContributionTestimonial
        {
            ContributionId = 5,
            Quote = "Role Hub finally made Copilot feel personal.",
            SpeakerName = "Field Seller Council",
            SpeakerRole = "Customer voice",
            Audience = TestimonialAudience.Customer,
            Sentiment = TestimonialSentiment.Positive
        };

        _contributionRepository
            .Setup(r => r.GetTestimonialsAsync())
            .ReturnsAsync([contribution]);

        var result = await _sut.GetTestimonialsAsync();

        result.Should().ContainSingle();
        var card = result[0];
        card.Id.Should().Be(5);
        card.InitiativeId.Should().Be(1);
        card.InitiativeName.Should().Be("Role Hub Rollout");
        card.Quote.Should().Be("Role Hub finally made Copilot feel personal.");
        card.SpeakerName.Should().Be("Field Seller Council");
        card.SpeakerRole.Should().Be("Customer voice");
        card.Audience.Should().Be(TestimonialAudience.Customer);
        card.Sentiment.Should().Be(TestimonialSentiment.Positive);
    }
}
