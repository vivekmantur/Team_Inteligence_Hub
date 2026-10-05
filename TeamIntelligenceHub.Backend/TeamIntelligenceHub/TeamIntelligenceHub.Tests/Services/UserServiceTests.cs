using FluentAssertions;
using Moq;
using TeamIntelligenceHub.Application.Interfaces;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Application.Services;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IInitiativeMemberRepository> _memberRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        // Default: nobody has any tracked allocation anywhere. Individual tests override
        // this when the total itself is what they're asserting on.
        _memberRepository
            .Setup(r => r.GetTotalAllocationByUserIdsAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new Dictionary<int, decimal>());

        _sut = new UserService(
            _userRepository.Object,
            _memberRepository.Object,
            _currentUserService.Object);
    }

    private static User CreateUser(
        int id = 1,
        string entraObjectId = "entra-object-id",
        string email = "user@example.com",
        string displayName = "User Name",
        bool isActive = true,
        DateTime? createdAt = null,
        DateTime? lastLoginAt = null)
    {
        return new User
        {
            Id = id,
            EntraObjectId = entraObjectId,
            Email = email,
            DisplayName = displayName,
            IsActive = isActive,
            CreatedAt = createdAt ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LastLoginAt = lastLoginAt ?? new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    private void SetupAuthenticatedCurrentUser(
        string entraObjectId = "entra-object-id",
        string email = "user@example.com",
        string? displayName = "User Name")
    {
        _currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserService.Setup(x => x.EntraObjectId).Returns(entraObjectId);
        _currentUserService.Setup(x => x.Email).Returns(email);
        _currentUserService.Setup(x => x.DisplayName).Returns(displayName);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_UserNotFound_ReturnsNull()
    {
        _userRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((User?)null);

        var result = await _sut.GetByIdAsync(1);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_UserExists_ReturnsMappedDto()
    {
        var user = CreateUser();
        _userRepository.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var result = await _sut.GetByIdAsync(user.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.EntraObjectId.Should().Be(user.EntraObjectId);
        result.Email.Should().Be(user.Email);
        result.DisplayName.Should().Be(user.DisplayName);
        result.IsActive.Should().Be(user.IsActive);
        result.CreatedAt.Should().Be(user.CreatedAt);
        result.LastLoginAt.Should().Be(user.LastLoginAt);
    }

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_UsersExist_ReturnsMappedList()
    {
        var users = new List<User>
        {
            CreateUser(id: 1, entraObjectId: "entra-1", email: "one@example.com"),
            CreateUser(id: 2, entraObjectId: "entra-2", email: "two@example.com")
        };
        _userRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(users);

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().ContainInOrder(1, 2);
        result[0].Email.Should().Be("one@example.com");
        result[1].Email.Should().Be("two@example.com");
    }

    [Fact]
    public async Task GetAllAsync_NoUsers_ReturnsEmptyList()
    {
        _userRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<User>());

        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    // ---------- GetCurrentUserAsync: authentication / claim validation ----------

    [Fact]
    public async Task GetCurrentUserAsync_NotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        _currentUserService.Setup(x => x.IsAuthenticated).Returns(false);

        var act = () => _sut.GetCurrentUserAsync();

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetCurrentUserAsync_EntraObjectIdMissing_ThrowsUnauthorizedAccessException(
        string? entraObjectId)
    {
        _currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserService.Setup(x => x.EntraObjectId).Returns(entraObjectId);

        var act = () => _sut.GetCurrentUserAsync();

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetCurrentUserAsync_EntraObjectIdTooLong_ThrowsUnauthorizedAccessException()
    {
        var tooLong = new string('a', User.EntraObjectIdMaxLength + 1);
        _currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserService.Setup(x => x.EntraObjectId).Returns(tooLong);

        var act = () => _sut.GetCurrentUserAsync();

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetCurrentUserAsync_EmailMissing_ThrowsUnauthorizedAccessException(
        string? email)
    {
        _currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserService.Setup(x => x.EntraObjectId).Returns("entra-object-id");
        _currentUserService.Setup(x => x.Email).Returns(email);

        var act = () => _sut.GetCurrentUserAsync();

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetCurrentUserAsync_EmailTooLong_ThrowsUnauthorizedAccessException()
    {
        var tooLongEmail = new string('a', User.EmailMaxLength) + "@example.com";
        _currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserService.Setup(x => x.EntraObjectId).Returns("entra-object-id");
        _currentUserService.Setup(x => x.Email).Returns(tooLongEmail);

        var act = () => _sut.GetCurrentUserAsync();

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Theory]
    [InlineData("no-at-sign")]
    [InlineData("@starts-with-at.com")]
    [InlineData("ends-with-at@")]
    public async Task GetCurrentUserAsync_EmailNotValidAddress_ThrowsUnauthorizedAccessException(
        string email)
    {
        _currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserService.Setup(x => x.EntraObjectId).Returns("entra-object-id");
        _currentUserService.Setup(x => x.Email).Returns(email);

        var act = () => _sut.GetCurrentUserAsync();

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ---------- GetCurrentUserAsync: create vs update ----------

    [Fact]
    public async Task GetCurrentUserAsync_UserDoesNotExist_CreatesNewActiveUserViaAddAsync()
    {
        SetupAuthenticatedCurrentUser(
            entraObjectId: "new-entra-id",
            email: "new.user@example.com",
            displayName: "New User");

        _userRepository
            .Setup(x => x.GetByEntraObjectIdAsync("new-entra-id"))
            .ReturnsAsync((User?)null);

        User? addedUser = null;
        _userRepository
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) =>
            {
                u.Id = 99;
                addedUser = u;
                return u;
            });

        var result = await _sut.GetCurrentUserAsync();

        addedUser.Should().NotBeNull();
        addedUser!.EntraObjectId.Should().Be("new-entra-id");
        addedUser.Email.Should().Be("new.user@example.com");
        addedUser.DisplayName.Should().Be("New User");
        addedUser.IsActive.Should().BeTrue();

        result.Id.Should().Be(99);
        result.Email.Should().Be("new.user@example.com");

        _userRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
        _userRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrentUserAsync_UserExists_UpdatesEmailDisplayNameAndLastLoginAtViaUpdateAsync()
    {
        var existingUser = CreateUser(
            id: 5,
            entraObjectId: "existing-entra-id",
            email: "old.email@example.com",
            displayName: "Old Name",
            lastLoginAt: new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        SetupAuthenticatedCurrentUser(
            entraObjectId: "existing-entra-id",
            email: "new.email@example.com",
            displayName: "New Name");

        _userRepository
            .Setup(x => x.GetByEntraObjectIdAsync("existing-entra-id"))
            .ReturnsAsync(existingUser);

        var result = await _sut.GetCurrentUserAsync();

        existingUser.Email.Should().Be("new.email@example.com");
        existingUser.DisplayName.Should().Be("New Name");
        existingUser.LastLoginAt.Should().NotBe(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        result.Id.Should().Be(5);
        result.Email.Should().Be("new.email@example.com");
        result.DisplayName.Should().Be("New Name");

        _userRepository.Verify(x => x.UpdateAsync(existingUser), Times.Once);
        _userRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    // ---------- GetCurrentUserAsync: display name handling ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetCurrentUserAsync_DisplayNameBlank_FallsBackToEmail(string? displayName)
    {
        SetupAuthenticatedCurrentUser(
            entraObjectId: "entra-object-id",
            email: "fallback@example.com",
            displayName: displayName);

        _userRepository
            .Setup(x => x.GetByEntraObjectIdAsync("entra-object-id"))
            .ReturnsAsync((User?)null);

        _userRepository
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        var result = await _sut.GetCurrentUserAsync();

        result.DisplayName.Should().Be("fallback@example.com");
    }

    [Fact]
    public async Task GetCurrentUserAsync_DisplayNameTooLong_TruncatedToMaxLength()
    {
        var tooLongDisplayName = new string('n', User.DisplayNameMaxLength + 50);

        SetupAuthenticatedCurrentUser(
            entraObjectId: "entra-object-id",
            email: "user@example.com",
            displayName: tooLongDisplayName);

        _userRepository
            .Setup(x => x.GetByEntraObjectIdAsync("entra-object-id"))
            .ReturnsAsync((User?)null);

        _userRepository
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        var result = await _sut.GetCurrentUserAsync();

        result.DisplayName.Should().HaveLength(User.DisplayNameMaxLength);
        result.DisplayName.Should().Be(tooLongDisplayName[..User.DisplayNameMaxLength]);
    }
}
