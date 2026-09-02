using FluentAssertions;
using Moq;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Application.Interfaces.Services;
using TeamIntelligenceHub.Application.Services;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Tests.Services;

public class ActivityServiceTests
{
    private readonly Mock<IActivityRepository> _activityRepository = new();
    private readonly Mock<IInitiativeRepository> _initiativeRepository = new();
    private readonly Mock<IInitiativeTaskService> _taskService = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private readonly ActivityService _sut;

    public ActivityServiceTests()
    {
        _sut = new ActivityService(
            _activityRepository.Object,
            _initiativeRepository.Object,
            _taskService.Object,
            _userRepository.Object,
            _currentUserService.Object);
    }

    // ----- helpers -----

    private static User CreateUser(int id, string entraObjectId = "entra-object-id")
    {
        return new User
        {
            Id = id,
            EntraObjectId = entraObjectId,
            Email = $"user{id}@example.com",
            DisplayName = $"User {id}"
        };
    }

    private void SetupCaller(User caller)
    {
        _currentUserService.Setup(c => c.EntraObjectId).Returns(caller.EntraObjectId);
        _userRepository
            .Setup(r => r.GetByEntraObjectIdAsync(caller.EntraObjectId))
            .ReturnsAsync(caller);
    }

    private void SetupInitiativeExists(int initiativeId)
    {
        _initiativeRepository
            .Setup(r => r.GetByIdAsync(initiativeId))
            .ReturnsAsync(new Initiative { Id = initiativeId });
    }

    private void SetupAddReturnsWithId(int id)
    {
        _activityRepository
            .Setup(r => r.AddAsync(It.IsAny<Activity>()))
            .ReturnsAsync((Activity a) =>
            {
                a.Id = id;
                return a;
            });
    }

    private void SetupReload(Activity activity)
    {
        _activityRepository
            .Setup(r => r.GetByIdAsync(activity.Id))
            .ReturnsAsync(activity);
    }

    private void SetupExistingUser(int userId)
    {
        _userRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(CreateUser(userId));
    }

    private static CreateActivityRequestDto CreateRequest(
        string message = "Hello team",
        bool autoCreateTaskEnabled = false,
        List<int>? mentionedUserIds = null)
    {
        return new CreateActivityRequestDto
        {
            ActivityMessage = message,
            AutoCreateTaskEnabled = autoCreateTaskEnabled,
            MentionedUserIds = mentionedUserIds
        };
    }

    // ----- GetByInitiativeAsync -----

    [Fact]
    public async Task GetByInitiativeAsync_InitiativeDoesNotExist_ThrowsNotFoundException()
    {
        _initiativeRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Initiative?)null);

        Func<Task> act = () => _sut.GetByInitiativeAsync(1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByInitiativeAsync_InitiativeExists_ReturnsMappedActivities()
    {
        SetupInitiativeExists(1);

        var activity = new Activity
        {
            Id = 10,
            InitiativeId = 1,
            UserId = 2,
            ActivityMessage = "Hello",
            AutoCreateTaskEnabled = false,
            CreatedAt = DateTime.UtcNow
        };

        _activityRepository
            .Setup(r => r.GetByInitiativeIdAsync(1))
            .ReturnsAsync([activity]);

        var result = await _sut.GetByInitiativeAsync(1);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(10);
        result[0].InitiativeId.Should().Be(1);
    }

    // ----- CreateAsync -----

    [Fact]
    public async Task CreateAsync_InitiativeDoesNotExist_ThrowsNotFoundException()
    {
        _initiativeRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Initiative?)null);

        Func<Task> act = () => _sut.CreateAsync(1, CreateRequest());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_MessageIsNullOrWhitespace_ThrowsValidationException(string? message)
    {
        SetupInitiativeExists(1);
        SetupCaller(CreateUser(42));

        Func<Task> act = () => _sut.CreateAsync(1, CreateRequest(message!));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_AuthorAlwaysComesFromCaller_NeverFromRequestBody()
    {
        SetupInitiativeExists(1);
        var caller = CreateUser(42);
        SetupCaller(caller);
        SetupAddReturnsWithId(100);

        var activity = new Activity
        {
            Id = 100,
            InitiativeId = 1,
            UserId = caller.Id,
            ActivityMessage = "Hello team",
            CreatedAt = DateTime.UtcNow
        };
        SetupReload(activity);

        await _sut.CreateAsync(1, CreateRequest("Hello team"));

        _activityRepository.Verify(
            r => r.AddAsync(It.Is<Activity>(a => a.UserId == caller.Id)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_MentionedUserIdsIsNull_ReplacesMentionsWithEmptyList()
    {
        SetupInitiativeExists(1);
        var caller = CreateUser(42);
        SetupCaller(caller);
        SetupAddReturnsWithId(100);
        SetupReload(new Activity { Id = 100, InitiativeId = 1, UserId = caller.Id, ActivityMessage = "Hi", CreatedAt = DateTime.UtcNow });

        await _sut.CreateAsync(1, CreateRequest(mentionedUserIds: null));

        _activityRepository.Verify(
            r => r.ReplaceMentionsAsync(100, It.Is<IReadOnlyCollection<int>>(ids => ids.Count == 0)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_MentionedUserIdsIsEmpty_ReplacesMentionsWithEmptyList()
    {
        SetupInitiativeExists(1);
        var caller = CreateUser(42);
        SetupCaller(caller);
        SetupAddReturnsWithId(100);
        SetupReload(new Activity { Id = 100, InitiativeId = 1, UserId = caller.Id, ActivityMessage = "Hi", CreatedAt = DateTime.UtcNow });

        await _sut.CreateAsync(1, CreateRequest(mentionedUserIds: []));

        _activityRepository.Verify(
            r => r.ReplaceMentionsAsync(100, It.Is<IReadOnlyCollection<int>>(ids => ids.Count == 0)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateMentionedUserIds_AreDeduplicatedBeforeReplacingMentions()
    {
        SetupInitiativeExists(1);
        var caller = CreateUser(42);
        SetupCaller(caller);
        SetupExistingUser(5);
        SetupExistingUser(6);
        SetupAddReturnsWithId(100);
        SetupReload(new Activity { Id = 100, InitiativeId = 1, UserId = caller.Id, ActivityMessage = "Hi", CreatedAt = DateTime.UtcNow });

        await _sut.CreateAsync(1, CreateRequest(mentionedUserIds: [5, 5, 6]));

        _activityRepository.Verify(
            r => r.ReplaceMentionsAsync(
                100,
                It.Is<IReadOnlyCollection<int>>(ids =>
                    ids.Count == 2 && ids.Contains(5) && ids.Contains(6))),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_MentionedUserDoesNotExist_ThrowsValidationException()
    {
        SetupInitiativeExists(1);
        SetupCaller(CreateUser(42));
        _userRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync((User?)null);

        Func<Task> act = () => _sut.CreateAsync(1, CreateRequest(mentionedUserIds: [5]));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_AutoCreateEnabled_CreatesTaskForEveryMentionedUserExceptAuthor()
    {
        var caller = CreateUser(1);
        SetupInitiativeExists(1);
        SetupCaller(caller);
        SetupExistingUser(1);
        SetupExistingUser(2);
        SetupExistingUser(3);
        SetupAddReturnsWithId(100);
        SetupReload(new Activity { Id = 100, InitiativeId = 1, UserId = caller.Id, ActivityMessage = "Hi", CreatedAt = DateTime.UtcNow });

        _taskService
            .Setup(t => t.CreateFromActivityAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new TaskResponseDto());

        await _sut.CreateAsync(
            1,
            CreateRequest("Hi", autoCreateTaskEnabled: true, mentionedUserIds: [1, 2, 3]));

        _taskService.Verify(
            t => t.CreateFromActivityAsync(1, 100, 2, It.IsAny<string>()),
            Times.Once);
        _taskService.Verify(
            t => t.CreateFromActivityAsync(1, 100, 3, It.IsAny<string>()),
            Times.Once);
        _taskService.Verify(
            t => t.CreateFromActivityAsync(1, 100, caller.Id, It.IsAny<string>()),
            Times.Never);
        _taskService.Verify(
            t => t.CreateFromActivityAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task CreateAsync_AutoCreateEnabled_TruncatesLongMessageTo80CharsAndTrimsEnd()
    {
        var caller = CreateUser(1);
        var longMessage = new string('a', 79) + "  " + "trailing rest that goes past the limit for sure";
        // The 80th character (index 79) lands inside the run of trailing spaces, so the
        // truncated slice should have its trailing whitespace trimmed off.
        SetupInitiativeExists(1);
        SetupCaller(caller);
        SetupExistingUser(2);
        SetupAddReturnsWithId(100);
        SetupReload(new Activity { Id = 100, InitiativeId = 1, UserId = caller.Id, ActivityMessage = longMessage, CreatedAt = DateTime.UtcNow });

        string? capturedTitle = null;
        _taskService
            .Setup(t => t.CreateFromActivityAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .Callback<int, int, int, string>((_, _, _, title) => capturedTitle = title)
            .ReturnsAsync(new TaskResponseDto());

        await _sut.CreateAsync(
            1,
            CreateRequest(longMessage, autoCreateTaskEnabled: true, mentionedUserIds: [2]));

        // First 80 characters are 79 'a's followed by a space (index 79), so after
        // TrimEnd() only the 79 'a's should remain.
        var expectedTitle = new string('a', 79);
        capturedTitle.Should().Be(expectedTitle);
    }

    [Fact]
    public async Task CreateAsync_AutoCreateDisabled_NeverCreatesTasks()
    {
        var caller = CreateUser(1);
        SetupInitiativeExists(1);
        SetupCaller(caller);
        SetupExistingUser(2);
        SetupAddReturnsWithId(100);
        SetupReload(new Activity { Id = 100, InitiativeId = 1, UserId = caller.Id, ActivityMessage = "Hi", CreatedAt = DateTime.UtcNow });

        await _sut.CreateAsync(
            1,
            CreateRequest("Hi", autoCreateTaskEnabled: false, mentionedUserIds: [2]));

        _taskService.Verify(
            t => t.CreateFromActivityAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never);
    }

    // ----- UpdateAsync -----

    [Fact]
    public async Task UpdateAsync_ActivityDoesNotExist_ThrowsNotFoundException()
    {
        _activityRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Activity?)null);

        Func<Task> act = () => _sut.UpdateAsync(1, 10, new UpdateActivityRequestDto { ActivityMessage = "Hi" });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ActivityBelongsToDifferentInitiative_ThrowsNotFoundException()
    {
        _activityRepository
            .Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(new Activity { Id = 10, InitiativeId = 99, UserId = 1, ActivityMessage = "Hi", CreatedAt = DateTime.UtcNow });

        Func<Task> act = () => _sut.UpdateAsync(1, 10, new UpdateActivityRequestDto { ActivityMessage = "Hi" });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_CallerIsNotTheAuthor_ThrowsValidationException()
    {
        var author = CreateUser(1);
        var otherCaller = CreateUser(2, "other-entra-id");

        _activityRepository
            .Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(new Activity { Id = 10, InitiativeId = 1, UserId = author.Id, ActivityMessage = "Hi", CreatedAt = DateTime.UtcNow });
        SetupCaller(otherCaller);

        Func<Task> act = () => _sut.UpdateAsync(1, 10, new UpdateActivityRequestDto { ActivityMessage = "Updated" });

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_NeverRaisesTasksEvenWhenAutoCreateTaskWasEnabledOnCreation()
    {
        var author = CreateUser(1);

        var activity = new Activity
        {
            Id = 10,
            InitiativeId = 1,
            UserId = author.Id,
            ActivityMessage = "Original",
            AutoCreateTaskEnabled = true,
            CreatedAt = DateTime.UtcNow
        };

        _activityRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(activity);
        SetupCaller(author);
        SetupExistingUser(2);

        await _sut.UpdateAsync(
            1,
            10,
            new UpdateActivityRequestDto { ActivityMessage = "Updated message", MentionedUserIds = [2] });

        _taskService.Verify(
            t => t.CreateFromActivityAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ValidRequestByAuthor_UpdatesActivityMessage()
    {
        var author = CreateUser(1);

        var activity = new Activity
        {
            Id = 10,
            InitiativeId = 1,
            UserId = author.Id,
            ActivityMessage = "Original",
            CreatedAt = DateTime.UtcNow
        };

        _activityRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(activity);
        SetupCaller(author);

        var result = await _sut.UpdateAsync(1, 10, new UpdateActivityRequestDto { ActivityMessage = "Updated message" });

        result.ActivityMessage.Should().Be("Updated message");
        _activityRepository.Verify(r => r.UpdateAsync(It.Is<Activity>(a => a.ActivityMessage == "Updated message")), Times.Once);
    }

    // ----- RemoveAsync -----

    [Fact]
    public async Task RemoveAsync_ActivityDoesNotExist_ThrowsNotFoundException()
    {
        _activityRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Activity?)null);

        Func<Task> act = () => _sut.RemoveAsync(1, 10);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemoveAsync_ActivityBelongsToDifferentInitiative_ThrowsNotFoundException()
    {
        _activityRepository
            .Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(new Activity { Id = 10, InitiativeId = 99, UserId = 1, ActivityMessage = "Hi", CreatedAt = DateTime.UtcNow });

        Func<Task> act = () => _sut.RemoveAsync(1, 10);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemoveAsync_CallerIsNotTheAuthor_ThrowsValidationException()
    {
        var author = CreateUser(1);
        var otherCaller = CreateUser(2, "other-entra-id");

        _activityRepository
            .Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(new Activity { Id = 10, InitiativeId = 1, UserId = author.Id, ActivityMessage = "Hi", CreatedAt = DateTime.UtcNow });
        SetupCaller(otherCaller);

        Func<Task> act = () => _sut.RemoveAsync(1, 10);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task RemoveAsync_CallerIsTheAuthor_RemovesActivity()
    {
        var author = CreateUser(1);

        var activity = new Activity
        {
            Id = 10,
            InitiativeId = 1,
            UserId = author.Id,
            ActivityMessage = "Hi",
            CreatedAt = DateTime.UtcNow
        };

        _activityRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(activity);
        SetupCaller(author);

        await _sut.RemoveAsync(1, 10);

        _activityRepository.Verify(r => r.RemoveAsync(activity), Times.Once);
    }
}
