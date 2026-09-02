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

public class InitiativeTaskServiceTests
{
    private const int InitiativeId = 1;
    private const int TaskId = 10;
    private const int CallerId = 100;
    private const int AssigneeId = 200;
    private const string CallerEntraObjectId = "caller-entra-object-id";

    private readonly Mock<IInitiativeTaskRepository> _taskRepository = new();
    private readonly Mock<IInitiativeRepository> _initiativeRepository = new();
    private readonly Mock<IInitiativeMemberRepository> _memberRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private readonly InitiativeTaskService _sut;

    public InitiativeTaskServiceTests()
    {
        _sut = new InitiativeTaskService(
            _taskRepository.Object,
            _initiativeRepository.Object,
            _memberRepository.Object,
            _userRepository.Object,
            _currentUserService.Object);
    }

    // ---------- Helpers ----------

    private void SetupInitiativeExists()
    {
        _initiativeRepository
            .Setup(r => r.GetByIdAsync(InitiativeId))
            .ReturnsAsync(new Initiative { Id = InitiativeId });
    }

    private void SetupCaller(int callerId = CallerId)
    {
        _currentUserService
            .Setup(c => c.EntraObjectId)
            .Returns(CallerEntraObjectId);

        _userRepository
            .Setup(r => r.GetByEntraObjectIdAsync(CallerEntraObjectId))
            .ReturnsAsync(new User
            {
                Id = callerId,
                EntraObjectId = CallerEntraObjectId,
                Email = "caller@example.com",
                DisplayName = "Caller",
                IsActive = true
            });
    }

    private void SetupActiveAssignee(int userId = AssigneeId)
    {
        _userRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(new User
            {
                Id = userId,
                EntraObjectId = $"entra-{userId}",
                Email = $"user{userId}@example.com",
                DisplayName = $"User {userId}",
                IsActive = true
            });
    }

    private void SetupDeactivatedAssignee(int userId = AssigneeId)
    {
        _userRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(new User
            {
                Id = userId,
                EntraObjectId = $"entra-{userId}",
                Email = $"user{userId}@example.com",
                DisplayName = $"User {userId}",
                IsActive = false
            });
    }

    private void SetupTaskAddPassthrough()
    {
        _taskRepository
            .Setup(r => r.AddAsync(It.IsAny<InitiativeTask>()))
            .ReturnsAsync((InitiativeTask task) =>
            {
                task.Id = TaskId;
                return task;
            });
    }

    private static CreateTaskRequestDto ValidCreateRequest(
        string title = "Write the design doc",
        int? assignedToUserId = null,
        DateOnly? dueDate = null) => new()
    {
        Title = title,
        AssignedToUserId = assignedToUserId,
        DueDate = dueDate
    };

    private static UpdateTaskRequestDto ValidUpdateRequest(
        string title = "Revise the design doc",
        int? assignedToUserId = null,
        DateOnly? dueDate = null) => new()
    {
        Title = title,
        AssignedToUserId = assignedToUserId,
        DueDate = dueDate
    };

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_InitiativeDoesNotExist_ThrowsNotFoundException()
    {
        _initiativeRepository
            .Setup(r => r.GetByIdAsync(InitiativeId))
            .ReturnsAsync((Initiative?)null);

        var act = () => _sut.CreateAsync(InitiativeId, ValidCreateRequest());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_TitleIsNullOrWhitespace_ThrowsValidationException(string? title)
    {
        SetupInitiativeExists();
        SetupCaller();

        var act = () => _sut.CreateAsync(InitiativeId, ValidCreateRequest(title: title!));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_DueDateBeforeEarliestDueDate_ThrowsValidationException()
    {
        SetupInitiativeExists();
        SetupCaller();

        var invalidDueDate = InitiativeTask.EarliestDueDate.AddDays(-1);
        var request = ValidCreateRequest(dueDate: invalidDueDate);

        var act = () => _sut.CreateAsync(InitiativeId, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_DueDateIsNull_DoesNotThrowAndPassesThrough()
    {
        SetupInitiativeExists();
        SetupCaller();
        SetupTaskAddPassthrough();

        var request = ValidCreateRequest(dueDate: null);

        var result = await _sut.CreateAsync(InitiativeId, request);

        result.DueDate.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_AssignedToUserDoesNotExist_ThrowsValidationException()
    {
        SetupInitiativeExists();
        SetupCaller();

        _userRepository
            .Setup(r => r.GetByIdAsync(AssigneeId))
            .ReturnsAsync((User?)null);

        var request = ValidCreateRequest(assignedToUserId: AssigneeId);

        var act = () => _sut.CreateAsync(InitiativeId, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_AssignedToUserIsDeactivated_ThrowsValidationException()
    {
        SetupInitiativeExists();
        SetupCaller();
        SetupDeactivatedAssignee();

        var request = ValidCreateRequest(assignedToUserId: AssigneeId);

        var act = () => _sut.CreateAsync(InitiativeId, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_AssignedToUserIdIsNull_TaskIsUnassignedAndSucceeds()
    {
        SetupInitiativeExists();
        SetupCaller();
        SetupTaskAddPassthrough();

        var request = ValidCreateRequest(assignedToUserId: null);

        var result = await _sut.CreateAsync(InitiativeId, request);

        result.AssignedToUserId.Should().BeNull();
        _memberRepository.Verify(
            m => m.AddAsync(It.IsAny<InitiativeMember>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_Always_SetsCreatedByUserIdFromCallerNotFromRequest()
    {
        SetupInitiativeExists();
        SetupCaller(CallerId);
        SetupTaskAddPassthrough();

        var request = ValidCreateRequest();

        await _sut.CreateAsync(InitiativeId, request);

        _taskRepository.Verify(
            r => r.AddAsync(It.Is<InitiativeTask>(t => t.CreatedByUserId == CallerId)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithAssigneeNotAlreadyOnTeam_EnrollsAssigneeAsTeamMember()
    {
        SetupInitiativeExists();
        SetupCaller();
        SetupActiveAssignee();
        SetupTaskAddPassthrough();

        _memberRepository
            .Setup(m => m.ExistsAsync(InitiativeId, AssigneeId))
            .ReturnsAsync(false);

        var request = ValidCreateRequest(assignedToUserId: AssigneeId);

        await _sut.CreateAsync(InitiativeId, request);

        _memberRepository.Verify(
            m => m.AddAsync(It.Is<InitiativeMember>(
                member => member.InitiativeId == InitiativeId && member.UserId == AssigneeId)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithAssigneeAlreadyOnTeam_DoesNotEnrollAssigneeAgain()
    {
        SetupInitiativeExists();
        SetupCaller();
        SetupActiveAssignee();
        SetupTaskAddPassthrough();

        _memberRepository
            .Setup(m => m.ExistsAsync(InitiativeId, AssigneeId))
            .ReturnsAsync(true);

        var request = ValidCreateRequest(assignedToUserId: AssigneeId);

        await _sut.CreateAsync(InitiativeId, request);

        _memberRepository.Verify(
            m => m.AddAsync(It.IsAny<InitiativeMember>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsCorrectlyMappedDto()
    {
        SetupInitiativeExists();
        SetupCaller(CallerId);
        SetupActiveAssignee();
        SetupTaskAddPassthrough();

        var dueDate = new DateOnly(2026, 1, 1);
        var request = ValidCreateRequest(
            title: "  Ship the feature  ",
            assignedToUserId: AssigneeId,
            dueDate: dueDate);
        request.Priority = InitiativeTaskPriority.High;
        request.Status = InitiativeTaskStatus.InProgress;

        var result = await _sut.CreateAsync(InitiativeId, request);

        result.Should().NotBeNull();
        result.Id.Should().Be(TaskId);
        result.InitiativeId.Should().Be(InitiativeId);
        result.Title.Should().Be("Ship the feature");
        result.AssignedToUserId.Should().Be(AssigneeId);
        result.CreatedByUserId.Should().Be(CallerId);
        result.DueDate.Should().Be(dueDate);
        result.Priority.Should().Be(InitiativeTaskPriority.High);
        result.Status.Should().Be(InitiativeTaskStatus.InProgress);
    }

    [Fact]
    public async Task CreateAsync_PriorityAndStatusOmitted_DefaultToMediumAndNotStarted()
    {
        SetupInitiativeExists();
        SetupCaller();
        SetupTaskAddPassthrough();

        var request = ValidCreateRequest();
        request.Priority = null;
        request.Status = null;

        var result = await _sut.CreateAsync(InitiativeId, request);

        result.Priority.Should().Be(InitiativeTaskPriority.Medium);
        result.Status.Should().Be(InitiativeTaskStatus.NotStarted);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_TaskDoesNotExist_ThrowsNotFoundException()
    {
        _taskRepository
            .Setup(r => r.GetByIdAsync(TaskId))
            .ReturnsAsync((InitiativeTask?)null);

        var act = () => _sut.UpdateAsync(InitiativeId, TaskId, ValidUpdateRequest());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_TaskBelongsToDifferentInitiative_ThrowsNotFoundException()
    {
        _taskRepository
            .Setup(r => r.GetByIdAsync(TaskId))
            .ReturnsAsync(new InitiativeTask
            {
                Id = TaskId,
                InitiativeId = InitiativeId + 999,
                Title = "Existing task"
            });

        var act = () => _sut.UpdateAsync(InitiativeId, TaskId, ValidUpdateRequest());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateAsync_TitleIsNullOrWhitespace_ThrowsValidationException(string? title)
    {
        _taskRepository
            .Setup(r => r.GetByIdAsync(TaskId))
            .ReturnsAsync(new InitiativeTask
            {
                Id = TaskId,
                InitiativeId = InitiativeId,
                Title = "Existing task"
            });

        var act = () => _sut.UpdateAsync(InitiativeId, TaskId, ValidUpdateRequest(title: title!));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_DueDateBeforeEarliestDueDate_ThrowsValidationException()
    {
        _taskRepository
            .Setup(r => r.GetByIdAsync(TaskId))
            .ReturnsAsync(new InitiativeTask
            {
                Id = TaskId,
                InitiativeId = InitiativeId,
                Title = "Existing task"
            });

        var invalidDueDate = InitiativeTask.EarliestDueDate.AddDays(-1);
        var request = ValidUpdateRequest(dueDate: invalidDueDate);

        var act = () => _sut.UpdateAsync(InitiativeId, TaskId, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_AssignedToUserDoesNotExist_ThrowsValidationException()
    {
        _taskRepository
            .Setup(r => r.GetByIdAsync(TaskId))
            .ReturnsAsync(new InitiativeTask
            {
                Id = TaskId,
                InitiativeId = InitiativeId,
                Title = "Existing task"
            });

        _userRepository
            .Setup(r => r.GetByIdAsync(AssigneeId))
            .ReturnsAsync((User?)null);

        var request = ValidUpdateRequest(assignedToUserId: AssigneeId);

        var act = () => _sut.UpdateAsync(InitiativeId, TaskId, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_AssignedToUserIsDeactivated_ThrowsValidationException()
    {
        _taskRepository
            .Setup(r => r.GetByIdAsync(TaskId))
            .ReturnsAsync(new InitiativeTask
            {
                Id = TaskId,
                InitiativeId = InitiativeId,
                Title = "Existing task"
            });

        SetupDeactivatedAssignee();

        var request = ValidUpdateRequest(assignedToUserId: AssigneeId);

        var act = () => _sut.UpdateAsync(InitiativeId, TaskId, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_WithAssigneeNotAlreadyOnTeam_EnrollsAssigneeAsTeamMember()
    {
        _taskRepository
            .Setup(r => r.GetByIdAsync(TaskId))
            .ReturnsAsync(new InitiativeTask
            {
                Id = TaskId,
                InitiativeId = InitiativeId,
                Title = "Existing task"
            });

        SetupActiveAssignee();

        _memberRepository
            .Setup(m => m.ExistsAsync(InitiativeId, AssigneeId))
            .ReturnsAsync(false);

        var request = ValidUpdateRequest(assignedToUserId: AssigneeId);

        await _sut.UpdateAsync(InitiativeId, TaskId, request);

        _memberRepository.Verify(
            m => m.AddAsync(It.Is<InitiativeMember>(
                member => member.InitiativeId == InitiativeId && member.UserId == AssigneeId)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithAssigneeAlreadyOnTeam_DoesNotEnrollAssigneeAgain()
    {
        _taskRepository
            .Setup(r => r.GetByIdAsync(TaskId))
            .ReturnsAsync(new InitiativeTask
            {
                Id = TaskId,
                InitiativeId = InitiativeId,
                Title = "Existing task"
            });

        SetupActiveAssignee();

        _memberRepository
            .Setup(m => m.ExistsAsync(InitiativeId, AssigneeId))
            .ReturnsAsync(true);

        var request = ValidUpdateRequest(assignedToUserId: AssigneeId);

        await _sut.UpdateAsync(InitiativeId, TaskId, request);

        _memberRepository.Verify(
            m => m.AddAsync(It.IsAny<InitiativeMember>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_ReturnsCorrectlyMappedDtoAndPersistsChanges()
    {
        var existingTask = new InitiativeTask
        {
            Id = TaskId,
            InitiativeId = InitiativeId,
            Title = "Old title",
            CreatedByUserId = CallerId,
            Priority = InitiativeTaskPriority.Low,
            Status = InitiativeTaskStatus.NotStarted,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        _taskRepository
            .Setup(r => r.GetByIdAsync(TaskId))
            .ReturnsAsync(existingTask);

        SetupActiveAssignee();

        var dueDate = new DateOnly(2026, 6, 1);
        var request = ValidUpdateRequest(
            title: "  New title  ",
            assignedToUserId: AssigneeId,
            dueDate: dueDate);
        request.Priority = InitiativeTaskPriority.High;
        request.Status = InitiativeTaskStatus.Done;

        var result = await _sut.UpdateAsync(InitiativeId, TaskId, request);

        result.Id.Should().Be(TaskId);
        result.Title.Should().Be("New title");
        result.AssignedToUserId.Should().Be(AssigneeId);
        result.DueDate.Should().Be(dueDate);
        result.Priority.Should().Be(InitiativeTaskPriority.High);
        result.Status.Should().Be(InitiativeTaskStatus.Done);
        result.UpdatedAt.Should().NotBeNull();

        _taskRepository.Verify(
            r => r.UpdateAsync(It.Is<InitiativeTask>(t => t.Id == TaskId)),
            Times.Once);
    }

    // ---------- CreateFromActivityAsync ----------

    [Fact]
    public async Task CreateFromActivityAsync_ValidRequest_SetsSourceActivityId()
    {
        SetupCaller();
        SetupActiveAssignee();
        SetupTaskAddPassthrough();

        const int activityId = 555;

        await _sut.CreateFromActivityAsync(InitiativeId, activityId, AssigneeId, "Follow up on mention");

        _taskRepository.Verify(
            r => r.AddAsync(It.Is<InitiativeTask>(t => t.SourceActivityId == activityId)),
            Times.Once);
    }

    [Fact]
    public async Task CreateFromActivityAsync_Always_SetsCreatedByUserIdFromCaller()
    {
        SetupCaller(CallerId);
        SetupActiveAssignee();
        SetupTaskAddPassthrough();

        await _sut.CreateFromActivityAsync(InitiativeId, 1, AssigneeId, "Follow up on mention");

        _taskRepository.Verify(
            r => r.AddAsync(It.Is<InitiativeTask>(t => t.CreatedByUserId == CallerId)),
            Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateFromActivityAsync_TitleIsNullOrWhitespace_ThrowsValidationException(string? title)
    {
        SetupCaller();
        SetupActiveAssignee();

        var act = () => _sut.CreateFromActivityAsync(InitiativeId, 1, AssigneeId, title!);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateFromActivityAsync_AssigneeDoesNotExist_ThrowsValidationException()
    {
        SetupCaller();

        _userRepository
            .Setup(r => r.GetByIdAsync(AssigneeId))
            .ReturnsAsync((User?)null);

        var act = () => _sut.CreateFromActivityAsync(InitiativeId, 1, AssigneeId, "Follow up");

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateFromActivityAsync_AssigneeIsDeactivated_ThrowsValidationException()
    {
        SetupCaller();
        SetupDeactivatedAssignee();

        var act = () => _sut.CreateFromActivityAsync(InitiativeId, 1, AssigneeId, "Follow up");

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateFromActivityAsync_AssigneeNotAlreadyOnTeam_EnrollsAssigneeAsTeamMember()
    {
        SetupCaller();
        SetupActiveAssignee();
        SetupTaskAddPassthrough();

        _memberRepository
            .Setup(m => m.ExistsAsync(InitiativeId, AssigneeId))
            .ReturnsAsync(false);

        await _sut.CreateFromActivityAsync(InitiativeId, 1, AssigneeId, "Follow up");

        _memberRepository.Verify(
            m => m.AddAsync(It.Is<InitiativeMember>(
                member => member.InitiativeId == InitiativeId && member.UserId == AssigneeId)),
            Times.Once);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_TaskDoesNotExist_ThrowsNotFoundException()
    {
        _taskRepository
            .Setup(r => r.GetByIdAsync(TaskId))
            .ReturnsAsync((InitiativeTask?)null);

        var act = () => _sut.GetByIdAsync(InitiativeId, TaskId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_TaskBelongsToDifferentInitiative_ThrowsNotFoundException()
    {
        _taskRepository
            .Setup(r => r.GetByIdAsync(TaskId))
            .ReturnsAsync(new InitiativeTask
            {
                Id = TaskId,
                InitiativeId = InitiativeId + 999,
                Title = "Existing task"
            });

        var act = () => _sut.GetByIdAsync(InitiativeId, TaskId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_TaskBelongsToInitiative_ReturnsMappedDto()
    {
        _taskRepository
            .Setup(r => r.GetByIdAsync(TaskId))
            .ReturnsAsync(new InitiativeTask
            {
                Id = TaskId,
                InitiativeId = InitiativeId,
                Title = "Existing task",
                CreatedByUserId = CallerId
            });

        var result = await _sut.GetByIdAsync(InitiativeId, TaskId);

        result.Id.Should().Be(TaskId);
        result.InitiativeId.Should().Be(InitiativeId);
        result.Title.Should().Be("Existing task");
    }

    // ---------- RemoveAsync ----------

    [Fact]
    public async Task RemoveAsync_TaskDoesNotExist_ThrowsNotFoundException()
    {
        _taskRepository
            .Setup(r => r.GetByIdAsync(TaskId))
            .ReturnsAsync((InitiativeTask?)null);

        var act = () => _sut.RemoveAsync(InitiativeId, TaskId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemoveAsync_TaskBelongsToDifferentInitiative_ThrowsNotFoundException()
    {
        _taskRepository
            .Setup(r => r.GetByIdAsync(TaskId))
            .ReturnsAsync(new InitiativeTask
            {
                Id = TaskId,
                InitiativeId = InitiativeId + 999,
                Title = "Existing task"
            });

        var act = () => _sut.RemoveAsync(InitiativeId, TaskId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemoveAsync_TaskBelongsToInitiative_RemovesTask()
    {
        var task = new InitiativeTask
        {
            Id = TaskId,
            InitiativeId = InitiativeId,
            Title = "Existing task"
        };

        _taskRepository
            .Setup(r => r.GetByIdAsync(TaskId))
            .ReturnsAsync(task);

        await _sut.RemoveAsync(InitiativeId, TaskId);

        _taskRepository.Verify(r => r.RemoveAsync(task), Times.Once);
    }

    // ---------- GetByInitiativeAsync ----------

    [Fact]
    public async Task GetByInitiativeAsync_InitiativeDoesNotExist_ThrowsNotFoundException()
    {
        _initiativeRepository
            .Setup(r => r.GetByIdAsync(InitiativeId))
            .ReturnsAsync((Initiative?)null);

        var act = () => _sut.GetByInitiativeAsync(InitiativeId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByInitiativeAsync_InitiativeExists_ReturnsMappedTasks()
    {
        SetupInitiativeExists();

        _taskRepository
            .Setup(r => r.GetByInitiativeIdAsync(InitiativeId))
            .ReturnsAsync(new List<InitiativeTask>
            {
                new()
                {
                    Id = TaskId,
                    InitiativeId = InitiativeId,
                    Title = "First task",
                    CreatedByUserId = CallerId
                }
            });

        var result = await _sut.GetByInitiativeAsync(InitiativeId);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(TaskId);
        result[0].Title.Should().Be("First task");
    }
}
