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

public class InitiativeServiceTests
{
    private const string DefaultEntraObjectId = "entra-object-id";

    private readonly Mock<IInitiativeRepository> _initiativeRepository = new();
    private readonly Mock<IContributionRepository> _contributionRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IFileStorage> _fileStorage = new();

    private readonly InitiativeService _sut;

    public InitiativeServiceTests()
    {
        _sut = new InitiativeService(
            _initiativeRepository.Object,
            _contributionRepository.Object,
            _userRepository.Object,
            _currentUserService.Object,
            _fileStorage.Object);
    }

    // ---------- helpers ----------

    private static User CreateUser(int id, bool isActive = true, string displayName = "Caller User")
    {
        return new User
        {
            Id = id,
            EntraObjectId = DefaultEntraObjectId,
            Email = $"user{id}@example.com",
            DisplayName = displayName,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static CreateInitiativeRequestDto CreateValidRequest()
    {
        return new CreateInitiativeRequestDto
        {
            Name = "Valid Initiative Name",
            Description = "A sufficiently long description of the initiative.",
            BusinessArea = InitiativeFocusArea.Enablement,
            InitiativeType = InitiativeWorkform.OperationalImprovement,
            StartDate = new DateOnly(2024, 1, 1),
            TargetEndDate = new DateOnly(2024, 6, 1)
        };
    }

    private void SetupCaller(User caller)
    {
        _currentUserService.Setup(x => x.EntraObjectId).Returns(DefaultEntraObjectId);
        _userRepository
            .Setup(x => x.GetByEntraObjectIdAsync(DefaultEntraObjectId))
            .ReturnsAsync(caller);
    }

    private static Initiative CreateInitiative(int id, User owner)
    {
        return new Initiative
        {
            Id = id,
            Name = "Existing Initiative",
            Description = "Existing description text.",
            BusinessArea = InitiativeFocusArea.StrategicPrograms,
            InitiativeType = InitiativeWorkform.Campaign,
            Priority = InitiativePriority.Medium,
            OwnerUserId = owner.Id,
            Owner = owner,
            Segment = InitiativeSegment.Enterprise,
            ChangeImpact = InitiativeChangeImpact.Medium,
            StartDate = new DateOnly(2024, 1, 1),
            TargetEndDate = new DateOnly(2024, 12, 31),
            LifecycleStage = InitiativeLifecycleStage.Plan,
            Health = InitiativeHealth.OnTrack,
            Status = InitiativeStatus.Active,
            CreatedAt = new DateTime(2024, 1, 1),
            UpdatedAt = new DateTime(2024, 1, 1)
        };
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_InitiativeDoesNotExist_ReturnsNull()
    {
        _initiativeRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync((Initiative?)null);

        var result = await _sut.GetByIdAsync(1);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_InitiativeExists_ReturnsMappedDto()
    {
        var owner = CreateUser(1, displayName: "Owner Person");
        var initiative = new Initiative
        {
            Id = 5,
            Name = "Existing Initiative",
            Description = "Existing description text.",
            BusinessArea = InitiativeFocusArea.StrategicPrograms,
            InitiativeType = InitiativeWorkform.Campaign,
            Priority = InitiativePriority.High,
            OwnerUserId = owner.Id,
            Owner = owner,
            StartDate = new DateOnly(2024, 1, 1),
            TargetEndDate = new DateOnly(2024, 12, 31),
            Status = InitiativeStatus.Active,
            CreatedAt = new DateTime(2024, 1, 1),
            UpdatedAt = new DateTime(2024, 1, 2)
        };

        _initiativeRepository
            .Setup(x => x.GetByIdAsync(5))
            .ReturnsAsync(initiative);

        var result = await _sut.GetByIdAsync(5);

        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        result.Name.Should().Be("Existing Initiative");
        result.OwnerUserId.Should().Be(owner.Id);
        result.OwnerDisplayName.Should().Be("Owner Person");
        result.Status.Should().Be(InitiativeStatus.Active);
    }

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_NoInitiatives_ReturnsEmptyList()
    {
        _initiativeRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Initiative>());

        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_InitiativesExist_ReturnsMappedList()
    {
        var owner = CreateUser(1, displayName: "Owner One");
        var initiatives = new List<Initiative>
        {
            new()
            {
                Id = 1,
                Name = "First",
                Description = "First description text.",
                BusinessArea = InitiativeFocusArea.StrategicPrograms,
                InitiativeType = InitiativeWorkform.Campaign,
                OwnerUserId = owner.Id,
                Owner = owner,
                StartDate = new DateOnly(2024, 1, 1),
                TargetEndDate = new DateOnly(2024, 6, 1)
            },
            new()
            {
                Id = 2,
                Name = "Second",
                Description = "Second description text.",
                BusinessArea = InitiativeFocusArea.StrategicPrograms,
                InitiativeType = InitiativeWorkform.Campaign,
                OwnerUserId = owner.Id,
                Owner = owner,
                StartDate = new DateOnly(2024, 1, 1),
                TargetEndDate = new DateOnly(2024, 6, 1)
            }
        };

        _initiativeRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(initiatives);

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().BeEquivalentTo(new[] { 1, 2 });
    }

    // ---------- CreateAsync: caller resolution ----------

    [Fact]
    public async Task CreateAsync_EntraObjectIdMissing_ThrowsUnauthorizedAccessException()
    {
        _currentUserService.Setup(x => x.EntraObjectId).Returns((string?)null);

        var request = CreateValidRequest();

        Func<Task> act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateAsync_EntraObjectIdIsWhitespace_ThrowsUnauthorizedAccessException()
    {
        _currentUserService.Setup(x => x.EntraObjectId).Returns("   ");

        var request = CreateValidRequest();

        Func<Task> act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateAsync_CallerUserRowDoesNotExist_ThrowsValidationException()
    {
        _currentUserService.Setup(x => x.EntraObjectId).Returns(DefaultEntraObjectId);
        _userRepository
            .Setup(x => x.GetByEntraObjectIdAsync(DefaultEntraObjectId))
            .ReturnsAsync((User?)null);

        var request = CreateValidRequest();

        Func<Task> act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ---------- CreateAsync: required text fields ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_NameMissingOrWhitespace_ThrowsValidationException(string? name)
    {
        var caller = CreateUser(1);
        SetupCaller(caller);

        var request = CreateValidRequest();
        request.Name = name!;

        Func<Task> act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_DescriptionMissingOrWhitespace_ThrowsValidationException(string? description)
    {
        var caller = CreateUser(1);
        SetupCaller(caller);

        var request = CreateValidRequest();
        request.Description = description!;

        Func<Task> act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_BusinessAreaNotProvided_ThrowsValidationException()
    {
        var caller = CreateUser(1);
        SetupCaller(caller);

        var request = CreateValidRequest();
        request.BusinessArea = null;

        Func<Task> act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_InitiativeTypeNotProvided_ThrowsValidationException()
    {
        var caller = CreateUser(1);
        SetupCaller(caller);

        var request = CreateValidRequest();
        request.InitiativeType = null;

        Func<Task> act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ---------- CreateAsync: dates ----------

    [Fact]
    public async Task CreateAsync_StartDateIsNull_ThrowsValidationException()
    {
        var caller = CreateUser(1);
        SetupCaller(caller);

        var request = CreateValidRequest();
        request.StartDate = null;

        Func<Task> act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_TargetEndDateIsNull_ThrowsValidationException()
    {
        var caller = CreateUser(1);
        SetupCaller(caller);

        var request = CreateValidRequest();
        request.TargetEndDate = null;

        Func<Task> act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_StartDateBeforeEarliestStartDate_ThrowsValidationException()
    {
        var caller = CreateUser(1);
        SetupCaller(caller);

        var request = CreateValidRequest();
        request.StartDate = Initiative.EarliestStartDate.AddDays(-1);
        request.TargetEndDate = Initiative.EarliestStartDate.AddDays(30);

        Func<Task> act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_TargetEndDateBeforeStartDate_ThrowsValidationException()
    {
        var caller = CreateUser(1);
        SetupCaller(caller);

        var request = CreateValidRequest();
        request.StartDate = new DateOnly(2024, 6, 1);
        request.TargetEndDate = new DateOnly(2024, 5, 1);

        Func<Task> act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_TargetEndDateExceedsMaxDuration_ThrowsValidationException()
    {
        var caller = CreateUser(1);
        SetupCaller(caller);

        var request = CreateValidRequest();
        request.StartDate = new DateOnly(2024, 1, 1);
        request.TargetEndDate = request.StartDate.Value
            .AddYears(Initiative.MaxDurationYears)
            .AddDays(1);

        Func<Task> act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_TargetEndDateExactlyAtMaxDuration_DoesNotThrowForDuration()
    {
        var caller = CreateUser(1);
        SetupCaller(caller);

        _userRepository
            .Setup(x => x.GetByIdAsync(caller.Id))
            .ReturnsAsync(caller);
        _initiativeRepository
            .Setup(x => x.NameExistsAsync(It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(false);
        _initiativeRepository
            .Setup(x => x.AddAsync(It.IsAny<Initiative>()))
            .ReturnsAsync((Initiative i) =>
            {
                i.Id = 1;
                return i;
            });

        var request = CreateValidRequest();
        request.StartDate = new DateOnly(2024, 1, 1);
        request.TargetEndDate = request.StartDate.Value.AddYears(Initiative.MaxDurationYears);

        Func<Task> act = () => _sut.CreateAsync(request);

        await act.Should().NotThrowAsync();
    }

    // ---------- CreateAsync: owner resolution ----------

    [Fact]
    public async Task CreateAsync_OwnerUserIdNotProvided_DefaultsToCallerId()
    {
        var caller = CreateUser(7);
        SetupCaller(caller);

        _userRepository
            .Setup(x => x.GetByIdAsync(caller.Id))
            .ReturnsAsync(caller);
        _initiativeRepository
            .Setup(x => x.NameExistsAsync(It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(false);

        Initiative? captured = null;
        _initiativeRepository
            .Setup(x => x.AddAsync(It.IsAny<Initiative>()))
            .ReturnsAsync((Initiative i) =>
            {
                i.Id = 1;
                captured = i;
                return i;
            });

        var request = CreateValidRequest();
        request.OwnerUserId = null;

        var result = await _sut.CreateAsync(request);

        result.OwnerUserId.Should().Be(caller.Id);
        captured!.OwnerUserId.Should().Be(caller.Id);
        _userRepository.Verify(x => x.GetByIdAsync(caller.Id), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_OwnerDoesNotExist_ThrowsValidationException()
    {
        var caller = CreateUser(1);
        SetupCaller(caller);

        _userRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((User?)null);

        var request = CreateValidRequest();
        request.OwnerUserId = 99;

        Func<Task> act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_OwnerIsInactive_ThrowsValidationException()
    {
        var caller = CreateUser(1);
        SetupCaller(caller);

        var inactiveOwner = CreateUser(2, isActive: false);
        _userRepository
            .Setup(x => x.GetByIdAsync(inactiveOwner.Id))
            .ReturnsAsync(inactiveOwner);

        var request = CreateValidRequest();
        request.OwnerUserId = inactiveOwner.Id;

        Func<Task> act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ---------- CreateAsync: executive sponsor ----------

    [Fact]
    public async Task CreateAsync_SponsorEqualsOwner_ThrowsValidationException()
    {
        var caller = CreateUser(1);
        SetupCaller(caller);

        _userRepository
            .Setup(x => x.GetByIdAsync(caller.Id))
            .ReturnsAsync(caller);

        var request = CreateValidRequest();
        request.OwnerUserId = caller.Id;
        request.ExecutiveSponsorUserId = caller.Id;

        Func<Task> act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_SponsorDoesNotExist_ThrowsValidationException()
    {
        var caller = CreateUser(1);
        SetupCaller(caller);

        _userRepository
            .Setup(x => x.GetByIdAsync(caller.Id))
            .ReturnsAsync(caller);
        _userRepository
            .Setup(x => x.GetByIdAsync(50))
            .ReturnsAsync((User?)null);

        var request = CreateValidRequest();
        request.OwnerUserId = caller.Id;
        request.ExecutiveSponsorUserId = 50;

        Func<Task> act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_SponsorIsInactive_ThrowsValidationException()
    {
        var caller = CreateUser(1);
        SetupCaller(caller);

        var inactiveSponsor = CreateUser(3, isActive: false);

        _userRepository
            .Setup(x => x.GetByIdAsync(caller.Id))
            .ReturnsAsync(caller);
        _userRepository
            .Setup(x => x.GetByIdAsync(inactiveSponsor.Id))
            .ReturnsAsync(inactiveSponsor);

        var request = CreateValidRequest();
        request.OwnerUserId = caller.Id;
        request.ExecutiveSponsorUserId = inactiveSponsor.Id;

        Func<Task> act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_SponsorExistsAndActive_MapsSponsorOnResult()
    {
        var caller = CreateUser(1);
        SetupCaller(caller);

        var sponsor = CreateUser(4, displayName: "Sponsor Person");

        _userRepository
            .Setup(x => x.GetByIdAsync(caller.Id))
            .ReturnsAsync(caller);
        _userRepository
            .Setup(x => x.GetByIdAsync(sponsor.Id))
            .ReturnsAsync(sponsor);
        _initiativeRepository
            .Setup(x => x.NameExistsAsync(It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(false);
        _initiativeRepository
            .Setup(x => x.AddAsync(It.IsAny<Initiative>()))
            .ReturnsAsync((Initiative i) =>
            {
                i.Id = 1;
                i.ExecutiveSponsor = sponsor;
                return i;
            });

        var request = CreateValidRequest();
        request.OwnerUserId = caller.Id;
        request.ExecutiveSponsorUserId = sponsor.Id;

        var result = await _sut.CreateAsync(request);

        result.ExecutiveSponsorUserId.Should().Be(sponsor.Id);
        result.ExecutiveSponsorDisplayName.Should().Be("Sponsor Person");
    }

    // ---------- CreateAsync: duplicate name ----------

    [Fact]
    public async Task CreateAsync_NameAlreadyExists_ThrowsValidationException()
    {
        var caller = CreateUser(1);
        SetupCaller(caller);

        _userRepository
            .Setup(x => x.GetByIdAsync(caller.Id))
            .ReturnsAsync(caller);
        _initiativeRepository
            .Setup(x => x.NameExistsAsync(It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(true);

        var request = CreateValidRequest();

        Func<Task> act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_NameHasSurroundingWhitespace_ChecksTrimmedNameForDuplicates()
    {
        var caller = CreateUser(1);
        SetupCaller(caller);

        _userRepository
            .Setup(x => x.GetByIdAsync(caller.Id))
            .ReturnsAsync(caller);
        _initiativeRepository
            .Setup(x => x.NameExistsAsync("Trimmed Name", It.IsAny<int?>()))
            .ReturnsAsync(false);
        _initiativeRepository
            .Setup(x => x.AddAsync(It.IsAny<Initiative>()))
            .ReturnsAsync((Initiative i) =>
            {
                i.Id = 1;
                return i;
            });

        var request = CreateValidRequest();
        request.Name = "  Trimmed Name  ";

        var result = await _sut.CreateAsync(request);

        result.Name.Should().Be("Trimmed Name");
        _initiativeRepository.Verify(
            x => x.NameExistsAsync("Trimmed Name", It.IsAny<int?>()), Times.Once);
    }

    // ---------- CreateAsync: defaults for optional enums ----------

    [Fact]
    public async Task CreateAsync_OptionalEnumsNotProvided_DefaultToActiveMediumAssessOnTrackEnterprise()
    {
        var caller = CreateUser(1);
        SetupCaller(caller);

        _userRepository
            .Setup(x => x.GetByIdAsync(caller.Id))
            .ReturnsAsync(caller);
        _initiativeRepository
            .Setup(x => x.NameExistsAsync(It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(false);
        _initiativeRepository
            .Setup(x => x.AddAsync(It.IsAny<Initiative>()))
            .ReturnsAsync((Initiative i) =>
            {
                i.Id = 1;
                return i;
            });

        var request = CreateValidRequest();
        request.Status = null;
        request.Priority = null;
        request.Segment = null;
        request.ChangeImpact = null;
        request.LifecycleStage = null;
        request.Health = null;

        var result = await _sut.CreateAsync(request);

        result.Status.Should().Be(InitiativeStatus.Active);
        result.Priority.Should().Be(InitiativePriority.Medium);
        result.Segment.Should().Be(InitiativeSegment.Enterprise);
        result.ChangeImpact.Should().Be(InitiativeChangeImpact.Medium);
        result.LifecycleStage.Should().Be(InitiativeLifecycleStage.Assess);
        result.Health.Should().Be(InitiativeHealth.OnTrack);
        result.ImpactedRoles.Should().BeEmpty();
    }

    // ---------- CreateAsync: successful creation mapping ----------

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsCorrectlyMappedDto()
    {
        var caller = CreateUser(1, displayName: "Owner Person");
        SetupCaller(caller);

        _userRepository
            .Setup(x => x.GetByIdAsync(caller.Id))
            .ReturnsAsync(caller);
        _initiativeRepository
            .Setup(x => x.NameExistsAsync(It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(false);

        Initiative? captured = null;
        _initiativeRepository
            .Setup(x => x.AddAsync(It.IsAny<Initiative>()))
            .ReturnsAsync((Initiative i) =>
            {
                i.Id = 123;
                i.Owner = caller;
                captured = i;
                return i;
            });

        var request = CreateValidRequest();
        request.Name = "  My New Initiative  ";
        request.Description = "  A description with padding.  ";
        request.BusinessArea = InitiativeFocusArea.InsightsAndMeasurement;
        request.InitiativeType = InitiativeWorkform.Pilot;
        request.Priority = InitiativePriority.High;
        request.Status = InitiativeStatus.Active;
        request.Segment = InitiativeSegment.Enterprise;
        request.ImpactedRoles = new List<EnterpriseRole> { EnterpriseRole.AE, EnterpriseRole.CSA };
        request.ChangeImpact = InitiativeChangeImpact.High;
        request.LifecycleStage = InitiativeLifecycleStage.Activate;
        request.Health = InitiativeHealth.AtRisk;
        request.KeyObjective = "  Objective  ";
        request.ExpectedOutcome = "   ";
        request.SuccessMeasures = null;

        var result = await _sut.CreateAsync(request);

        result.Id.Should().Be(123);
        result.Name.Should().Be("My New Initiative");
        result.Description.Should().Be("A description with padding.");
        result.BusinessArea.Should().Be(InitiativeFocusArea.InsightsAndMeasurement);
        result.InitiativeType.Should().Be(InitiativeWorkform.Pilot);
        result.Priority.Should().Be(InitiativePriority.High);
        result.Status.Should().Be(InitiativeStatus.Active);
        result.Segment.Should().Be(InitiativeSegment.Enterprise);
        result.ImpactedRoles.Should().BeEquivalentTo(new[] { EnterpriseRole.AE, EnterpriseRole.CSA });
        result.ChangeImpact.Should().Be(InitiativeChangeImpact.High);
        result.LifecycleStage.Should().Be(InitiativeLifecycleStage.Activate);
        result.Health.Should().Be(InitiativeHealth.AtRisk);
        result.OwnerUserId.Should().Be(caller.Id);
        result.OwnerDisplayName.Should().Be("Owner Person");
        result.KeyObjective.Should().Be("Objective");
        result.ExpectedOutcome.Should().BeNull();
        result.SuccessMeasures.Should().BeNull();

        captured.Should().NotBeNull();
        captured!.Name.Should().Be("My New Initiative");
        captured.BusinessArea.Should().Be(InitiativeFocusArea.InsightsAndMeasurement);
        captured.InitiativeType.Should().Be(InitiativeWorkform.Pilot);
    }

    // ---------- UpdateAsync ----------

    private static UpdateInitiativeRequestDto CreateValidUpdateRequest()
    {
        return new UpdateInitiativeRequestDto
        {
            Name = "Valid Initiative Name",
            Description = "A sufficiently long description of the initiative.",
            BusinessArea = InitiativeFocusArea.Enablement,
            InitiativeType = InitiativeWorkform.OperationalImprovement,
            StartDate = new DateOnly(2024, 1, 1),
            TargetEndDate = new DateOnly(2024, 6, 1)
        };
    }

    [Fact]
    public async Task UpdateAsync_InitiativeDoesNotExist_ThrowsNotFoundException()
    {
        _initiativeRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Initiative?)null);

        var act = () => _sut.UpdateAsync(1, CreateValidUpdateRequest());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_PersistsAndReturnsMappedDto()
    {
        var owner = CreateUser(1, displayName: "Owner Person");
        var newOwner = CreateUser(2, displayName: "New Owner");
        var initiative = CreateInitiative(5, owner);

        _initiativeRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(initiative);
        _userRepository.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(newOwner);
        _initiativeRepository
            .Setup(x => x.NameExistsAsync("Renamed Initiative", 5))
            .ReturnsAsync(false);

        var request = CreateValidUpdateRequest();
        request.Name = "Renamed Initiative";
        request.OwnerUserId = 2;

        var result = await _sut.UpdateAsync(5, request);

        _initiativeRepository.Verify(x => x.UpdateAsync(initiative), Times.Once);
        initiative.Name.Should().Be("Renamed Initiative");
        initiative.OwnerUserId.Should().Be(2);
        result.Id.Should().Be(5);
        result.Name.Should().Be("Renamed Initiative");
    }

    [Fact]
    public async Task UpdateAsync_NameUnchanged_NeverChecksForADuplicate()
    {
        var owner = CreateUser(1);
        var initiative = CreateInitiative(5, owner);

        _initiativeRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(initiative);
        _userRepository.Setup(x => x.GetByIdAsync(owner.Id)).ReturnsAsync(owner);

        var request = CreateValidUpdateRequest();
        request.Name = initiative.Name;
        request.OwnerUserId = owner.Id;

        await _sut.UpdateAsync(5, request);

        _initiativeRepository.Verify(
            x => x.NameExistsAsync(It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_NameChangedToAnotherInitiativesName_ThrowsValidationException()
    {
        var owner = CreateUser(1);
        var initiative = CreateInitiative(5, owner);

        _initiativeRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(initiative);
        _userRepository.Setup(x => x.GetByIdAsync(owner.Id)).ReturnsAsync(owner);
        _initiativeRepository
            .Setup(x => x.NameExistsAsync("Taken Name", 5))
            .ReturnsAsync(true);

        var request = CreateValidUpdateRequest();
        request.Name = "Taken Name";
        request.OwnerUserId = owner.Id;

        var act = () => _sut.UpdateAsync(5, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_OwnerDoesNotExist_ThrowsValidationException()
    {
        var owner = CreateUser(1);
        var initiative = CreateInitiative(5, owner);

        _initiativeRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(initiative);
        _userRepository.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((User?)null);

        var request = CreateValidUpdateRequest();
        request.OwnerUserId = 999;

        var act = () => _sut.UpdateAsync(5, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_OmittedOptionalFields_PreservesTheInitiativesCurrentValues()
    {
        var owner = CreateUser(1);
        var initiative = CreateInitiative(5, owner);
        initiative.Priority = InitiativePriority.High;
        initiative.Health = InitiativeHealth.AtRisk;

        _initiativeRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(initiative);
        _userRepository.Setup(x => x.GetByIdAsync(owner.Id)).ReturnsAsync(owner);
        _initiativeRepository
            .Setup(x => x.NameExistsAsync(It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(false);

        var request = CreateValidUpdateRequest();
        request.OwnerUserId = owner.Id;
        // Priority and Health left null — an omission during Update means "unchanged",
        // unlike Create, where the same omission would apply a fixed default.
        request.Priority = null;
        request.Health = null;

        await _sut.UpdateAsync(5, request);

        initiative.Priority.Should().Be(InitiativePriority.High);
        initiative.Health.Should().Be(InitiativeHealth.AtRisk);
    }

    // ---------- GetDeletionImpactAsync ----------

    [Fact]
    public async Task GetDeletionImpactAsync_InitiativeDoesNotExist_ThrowsNotFoundException()
    {
        _initiativeRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Initiative?)null);

        var act = () => _sut.GetDeletionImpactAsync(1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetDeletionImpactAsync_ReturnsCountsFromTheRepository()
    {
        var owner = CreateUser(1);
        _initiativeRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(CreateInitiative(5, owner));
        _initiativeRepository
            .Setup(x => x.GetDeletionImpactAsync(5))
            .ReturnsAsync((Contributions: 3, Tasks: 5, Activities: 12, Members: 2));

        var result = await _sut.GetDeletionImpactAsync(5);

        result.ContributionCount.Should().Be(3);
        result.TaskCount.Should().Be(5);
        result.ActivityCount.Should().Be(12);
        result.TeamMemberCount.Should().Be(2);
    }

    // ---------- RemoveAsync ----------

    [Fact]
    public async Task RemoveAsync_InitiativeDoesNotExist_ThrowsNotFoundException()
    {
        _initiativeRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Initiative?)null);

        var act = () => _sut.RemoveAsync(1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemoveAsync_DeletesEveryContributionAttachmentBlob_ThenRemovesTheInitiative()
    {
        var owner = CreateUser(1);
        var initiative = CreateInitiative(5, owner);
        var contributions = new List<Contribution>
        {
            new()
            {
                Id = 10,
                InitiativeId = 5,
                Attachments = new List<ContributionAttachment>
                {
                    new() { Id = 1, ContributionId = 10, FileName = "a.pdf", BlobName = "blob-a", ContentType = "application/pdf" },
                    new() { Id = 2, ContributionId = 10, FileName = "b.pdf", BlobName = "blob-b", ContentType = "application/pdf" }
                }
            },
            new()
            {
                Id = 11,
                InitiativeId = 5,
                Attachments = new List<ContributionAttachment>
                {
                    new() { Id = 3, ContributionId = 11, FileName = "c.pdf", BlobName = "blob-c", ContentType = "application/pdf" }
                }
            }
        };

        _initiativeRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(initiative);
        _contributionRepository.Setup(x => x.GetByInitiativeIdAsync(5)).ReturnsAsync(contributions);

        await _sut.RemoveAsync(5);

        _fileStorage.Verify(
            x => x.DeleteAsync(
                FileStorageArea.ContributionAttachments, "blob-a", It.IsAny<CancellationToken>()),
            Times.Once);
        _fileStorage.Verify(
            x => x.DeleteAsync(
                FileStorageArea.ContributionAttachments, "blob-b", It.IsAny<CancellationToken>()),
            Times.Once);
        _fileStorage.Verify(
            x => x.DeleteAsync(
                FileStorageArea.ContributionAttachments, "blob-c", It.IsAny<CancellationToken>()),
            Times.Once);
        _initiativeRepository.Verify(x => x.RemoveAsync(initiative), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_NoContributions_StillRemovesTheInitiative()
    {
        var owner = CreateUser(1);
        var initiative = CreateInitiative(5, owner);

        _initiativeRepository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(initiative);
        _contributionRepository
            .Setup(x => x.GetByInitiativeIdAsync(5))
            .ReturnsAsync(new List<Contribution>());

        await _sut.RemoveAsync(5);

        _fileStorage.Verify(
            x => x.DeleteAsync(
                It.IsAny<FileStorageArea>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _initiativeRepository.Verify(x => x.RemoveAsync(initiative), Times.Once);
    }
}
