using FluentAssertions;
using Moq;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Application.Services;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Tests.Services;

public class InitiativeMemberServiceTests
{
    private readonly Mock<IInitiativeMemberRepository> _memberRepository = new();
    private readonly Mock<IInitiativeRepository> _initiativeRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();

    private readonly InitiativeMemberService _sut;

    public InitiativeMemberServiceTests()
    {
        _sut = new InitiativeMemberService(
            _memberRepository.Object,
            _initiativeRepository.Object,
            _userRepository.Object);
    }

    private static Initiative CreateInitiative(int id = 1)
    {
        return new Initiative { Id = id };
    }

    private static User CreateUser(
        int id = 1,
        bool isActive = true,
        string displayName = "Jane Doe",
        string email = "jane.doe@example.com")
    {
        return new User
        {
            Id = id,
            DisplayName = displayName,
            Email = email,
            IsActive = isActive,
            EntraObjectId = "entra-object-id"
        };
    }

    private static InitiativeMember CreateMember(
        int id = 1,
        int initiativeId = 1,
        int userId = 1,
        string role = "Developer",
        User? user = null)
    {
        return new InitiativeMember
        {
            Id = id,
            InitiativeId = initiativeId,
            UserId = userId,
            Role = role,
            ResponsibilityArea = "Backend",
            Allocation = 50m,
            JoinedAt = DateTime.UtcNow,
            User = user ?? CreateUser(userId)
        };
    }

    // ---------------------------------------------------------------
    // GetByInitiativeAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetByInitiativeAsync_InitiativeDoesNotExist_ThrowsNotFoundException()
    {
        _initiativeRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync((Initiative?)null);

        var act = () => _sut.GetByInitiativeAsync(1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByInitiativeAsync_InitiativeExists_ReturnsMappedMembers()
    {
        var initiative = CreateInitiative(1);
        var user = CreateUser(2, displayName: "John Smith", email: "john.smith@example.com");
        var member = CreateMember(id: 10, initiativeId: 1, userId: 2, role: "Analyst", user: user);

        _initiativeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(initiative);
        _memberRepository
            .Setup(r => r.GetByInitiativeIdAsync(1))
            .ReturnsAsync(new List<InitiativeMember> { member });

        var result = await _sut.GetByInitiativeAsync(1);

        result.Should().HaveCount(1);
        var dto = result[0];
        dto.Id.Should().Be(10);
        dto.InitiativeId.Should().Be(1);
        dto.UserId.Should().Be(2);
        dto.UserDisplayName.Should().Be("John Smith");
        dto.UserEmail.Should().Be("john.smith@example.com");
        dto.Role.Should().Be("Analyst");
    }

    // ---------------------------------------------------------------
    // AddAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task AddAsync_InitiativeDoesNotExist_ThrowsNotFoundException()
    {
        _initiativeRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync((Initiative?)null);

        var request = new AddInitiativeMemberRequestDto { UserId = 1, Role = "Developer" };

        var act = () => _sut.AddAsync(1, request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddAsync_UserIdIsNull_ThrowsValidationException()
    {
        _initiativeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateInitiative(1));

        var request = new AddInitiativeMemberRequestDto { UserId = null, Role = "Developer" };

        var act = () => _sut.AddAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddAsync_UserDoesNotExist_ThrowsValidationException()
    {
        _initiativeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateInitiative(1));
        _userRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync((User?)null);

        var request = new AddInitiativeMemberRequestDto { UserId = 5, Role = "Developer" };

        var act = () => _sut.AddAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddAsync_UserIsInactive_ThrowsValidationException()
    {
        _initiativeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateInitiative(1));
        _userRepository
            .Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(CreateUser(5, isActive: false));

        var request = new AddInitiativeMemberRequestDto { UserId = 5, Role = "Developer" };

        var act = () => _sut.AddAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddAsync_UserAlreadyMemberOfInitiative_ThrowsValidationException()
    {
        _initiativeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateInitiative(1));
        _userRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(CreateUser(5));
        _memberRepository.Setup(r => r.ExistsAsync(1, 5)).ReturnsAsync(true);

        var request = new AddInitiativeMemberRequestDto { UserId = 5, Role = "Developer" };

        var act = () => _sut.AddAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddAsync_RoleIsNullOrWhitespace_ThrowsValidationException(string? role)
    {
        _initiativeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateInitiative(1));
        _userRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(CreateUser(5));
        _memberRepository.Setup(r => r.ExistsAsync(1, 5)).ReturnsAsync(false);

        var request = new AddInitiativeMemberRequestDto { UserId = 5, Role = role! };

        var act = () => _sut.AddAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData("Other")]
    [InlineData("other")]
    [InlineData(" OTHER ")]
    public async Task AddAsync_RoleIsPlaceholderOther_ThrowsValidationException(string role)
    {
        _initiativeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateInitiative(1));
        _userRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(CreateUser(5));
        _memberRepository.Setup(r => r.ExistsAsync(1, 5)).ReturnsAsync(false);

        var request = new AddInitiativeMemberRequestDto { UserId = 5, Role = role };

        var act = () => _sut.AddAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddAsync_RoleHasSurroundingWhitespace_TrimsAndStoresRole()
    {
        _initiativeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateInitiative(1));
        _userRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(CreateUser(5));
        _memberRepository.Setup(r => r.ExistsAsync(1, 5)).ReturnsAsync(false);
        _memberRepository
            .Setup(r => r.AddAsync(It.IsAny<InitiativeMember>()))
            .ReturnsAsync((InitiativeMember m) => m);

        var request = new AddInitiativeMemberRequestDto
        {
            UserId = 5,
            Role = "  Team Lead  "
        };

        var result = await _sut.AddAsync(1, request);

        result.Role.Should().Be("Team Lead");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100.01)]
    [InlineData(150)]
    public async Task AddAsync_AllocationOutsideRange_ThrowsValidationException(decimal allocation)
    {
        _initiativeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateInitiative(1));
        _userRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(CreateUser(5));
        _memberRepository.Setup(r => r.ExistsAsync(1, 5)).ReturnsAsync(false);

        var request = new AddInitiativeMemberRequestDto
        {
            UserId = 5,
            Role = "Developer",
            Allocation = allocation
        };

        var act = () => _sut.AddAsync(1, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddAsync_AllocationIsNull_PassesThroughAsNull()
    {
        _initiativeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateInitiative(1));
        _userRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(CreateUser(5));
        _memberRepository.Setup(r => r.ExistsAsync(1, 5)).ReturnsAsync(false);
        _memberRepository
            .Setup(r => r.AddAsync(It.IsAny<InitiativeMember>()))
            .ReturnsAsync((InitiativeMember m) => m);

        var request = new AddInitiativeMemberRequestDto
        {
            UserId = 5,
            Role = "Developer",
            Allocation = null
        };

        var result = await _sut.AddAsync(1, request);

        result.Allocation.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_AllocationHasMoreThanTwoDecimals_RoundsToTwoDecimalPlaces()
    {
        _initiativeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateInitiative(1));
        _userRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(CreateUser(5));
        _memberRepository.Setup(r => r.ExistsAsync(1, 5)).ReturnsAsync(false);
        _memberRepository
            .Setup(r => r.AddAsync(It.IsAny<InitiativeMember>()))
            .ReturnsAsync((InitiativeMember m) => m);

        var request = new AddInitiativeMemberRequestDto
        {
            UserId = 5,
            Role = "Developer",
            Allocation = 33.336m
        };

        var result = await _sut.AddAsync(1, request);

        result.Allocation.Should().Be(33.34m);
    }

    [Fact]
    public async Task AddAsync_ValidRequest_ReturnsMappedResponseDto()
    {
        var initiative = CreateInitiative(1);
        var user = CreateUser(5, displayName: "Alice Example", email: "alice@example.com");

        _initiativeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(initiative);
        _userRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);
        _memberRepository.Setup(r => r.ExistsAsync(1, 5)).ReturnsAsync(false);
        _memberRepository
            .Setup(r => r.AddAsync(It.IsAny<InitiativeMember>()))
            .ReturnsAsync((InitiativeMember m) =>
            {
                m.Id = 42;
                m.User = user;
                return m;
            });

        var request = new AddInitiativeMemberRequestDto
        {
            UserId = 5,
            Role = "Developer",
            ResponsibilityArea = "Frontend",
            Allocation = 75m
        };

        var result = await _sut.AddAsync(1, request);

        result.Id.Should().Be(42);
        result.InitiativeId.Should().Be(1);
        result.UserId.Should().Be(5);
        result.UserDisplayName.Should().Be("Alice Example");
        result.UserEmail.Should().Be("alice@example.com");
        result.Role.Should().Be("Developer");
        result.ResponsibilityArea.Should().Be("Frontend");
        result.Allocation.Should().Be(75m);
    }

    // ---------------------------------------------------------------
    // UpdateAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_MemberDoesNotExist_ThrowsNotFoundException()
    {
        _memberRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((InitiativeMember?)null);

        var request = new UpdateInitiativeMemberRequestDto { Role = "Developer" };

        var act = () => _sut.UpdateAsync(1, 10, request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_MemberBelongsToDifferentInitiative_ThrowsNotFoundException()
    {
        var member = CreateMember(id: 10, initiativeId: 2);
        _memberRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(member);

        var request = new UpdateInitiativeMemberRequestDto { Role = "Developer" };

        var act = () => _sut.UpdateAsync(1, 10, request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateAsync_RoleIsNullOrWhitespace_ThrowsValidationException(string? role)
    {
        var member = CreateMember(id: 10, initiativeId: 1);
        _memberRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(member);

        var request = new UpdateInitiativeMemberRequestDto { Role = role! };

        var act = () => _sut.UpdateAsync(1, 10, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_RoleIsPlaceholderOther_ThrowsValidationException()
    {
        var member = CreateMember(id: 10, initiativeId: 1);
        _memberRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(member);

        var request = new UpdateInitiativeMemberRequestDto { Role = "other" };

        var act = () => _sut.UpdateAsync(1, 10, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.5)]
    public async Task UpdateAsync_AllocationOutsideRange_ThrowsValidationException(decimal allocation)
    {
        var member = CreateMember(id: 10, initiativeId: 1);
        _memberRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(member);

        var request = new UpdateInitiativeMemberRequestDto
        {
            Role = "Developer",
            Allocation = allocation
        };

        var act = () => _sut.UpdateAsync(1, 10, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_ReturnsMappedResponseDtoAndPersists()
    {
        var user = CreateUser(5, displayName: "Bob Example", email: "bob@example.com");
        var member = CreateMember(id: 10, initiativeId: 1, userId: 5, user: user);
        _memberRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(member);
        _memberRepository.Setup(r => r.UpdateAsync(member)).Returns(Task.CompletedTask);

        var request = new UpdateInitiativeMemberRequestDto
        {
            Role = "  Senior Developer  ",
            ResponsibilityArea = "Platform",
            Allocation = 60.006m
        };

        var result = await _sut.UpdateAsync(1, 10, request);

        result.Id.Should().Be(10);
        result.InitiativeId.Should().Be(1);
        result.UserId.Should().Be(5);
        result.UserDisplayName.Should().Be("Bob Example");
        result.UserEmail.Should().Be("bob@example.com");
        result.Role.Should().Be("Senior Developer");
        result.ResponsibilityArea.Should().Be("Platform");
        result.Allocation.Should().Be(60.01m);
        _memberRepository.Verify(r => r.UpdateAsync(member), Times.Once);
    }

    // ---------------------------------------------------------------
    // RemoveAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task RemoveAsync_MemberDoesNotExist_ThrowsNotFoundException()
    {
        _memberRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((InitiativeMember?)null);

        var act = () => _sut.RemoveAsync(1, 10);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemoveAsync_MemberBelongsToDifferentInitiative_ThrowsNotFoundException()
    {
        var member = CreateMember(id: 10, initiativeId: 2);
        _memberRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(member);

        var act = () => _sut.RemoveAsync(1, 10);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemoveAsync_MemberExistsInInitiative_RemovesMember()
    {
        var member = CreateMember(id: 10, initiativeId: 1);
        _memberRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(member);
        _memberRepository.Setup(r => r.RemoveAsync(member)).Returns(Task.CompletedTask);

        await _sut.RemoveAsync(1, 10);

        _memberRepository.Verify(r => r.RemoveAsync(member), Times.Once);
    }
}
