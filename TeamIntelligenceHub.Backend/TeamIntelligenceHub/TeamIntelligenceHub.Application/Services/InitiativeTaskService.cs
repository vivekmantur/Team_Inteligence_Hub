using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Application.Interfaces.Services;
using TeamIntelligenceHub.Domain.Entities;
using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Application.Services;

public class InitiativeTaskService : IInitiativeTaskService
{
    /// <summary>
    /// Role given to someone enrolled on the team purely by being assigned work. It is a
    /// preset value, so it round-trips through the UI as a known role rather than "Other".
    /// </summary>
    private const string DefaultAssigneeRole = "Contributor";

    private readonly IInitiativeTaskRepository _taskRepository;
    private readonly IInitiativeRepository _initiativeRepository;
    private readonly IInitiativeMemberRepository _memberRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public InitiativeTaskService(
        IInitiativeTaskRepository taskRepository,
        IInitiativeRepository initiativeRepository,
        IInitiativeMemberRepository memberRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _taskRepository = taskRepository;
        _initiativeRepository = initiativeRepository;
        _memberRepository = memberRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<TaskResponseDto>> GetByInitiativeAsync(int initiativeId)
    {
        await RequireInitiativeAsync(initiativeId);

        var tasks = await _taskRepository.GetByInitiativeIdAsync(initiativeId);

        return tasks
            .Select(MapToDto)
            .ToList();
    }

    public async Task<TaskResponseDto> GetByIdAsync(int initiativeId, int taskId)
    {
        var task = await RequireTaskAsync(initiativeId, taskId);

        return MapToDto(task);
    }

    public async Task<TaskResponseDto> CreateAsync(
        int initiativeId,
        CreateTaskRequestDto request)
    {
        await RequireInitiativeAsync(initiativeId);

        var caller = await GetCallerAsync();

        var task = new InitiativeTask
        {
            InitiativeId = initiativeId,
            Title = RequireTitle(request.Title),
            AssignedToUserId = await ResolveAssigneeAsync(request.AssignedToUserId),
            // The creator is whoever holds the token, never a value from the body.
            CreatedByUserId = caller.Id,
            DueDate = RequireDueDate(request.DueDate),
            Priority = request.Priority ?? InitiativeTaskPriority.Medium,
            Status = request.Status ?? InitiativeTaskStatus.NotStarted,
            CreatedAt = DateTime.UtcNow,
            // Null until the task is first edited.
            UpdatedAt = null
        };

        var created = await _taskRepository.AddAsync(task);

        await EnsureTeamMembershipAsync(initiativeId, created.AssignedToUserId);

        return MapToDto(created);
    }

    public async Task<TaskResponseDto> UpdateAsync(
        int initiativeId,
        int taskId,
        UpdateTaskRequestDto request)
    {
        var task = await RequireTaskAsync(initiativeId, taskId);

        task.Title = RequireTitle(request.Title);
        task.AssignedToUserId = await ResolveAssigneeAsync(request.AssignedToUserId);
        task.DueDate = RequireDueDate(request.DueDate);
        task.Priority = request.Priority ?? task.Priority;
        task.Status = request.Status ?? task.Status;
        task.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.UpdateAsync(task);

        await EnsureTeamMembershipAsync(initiativeId, task.AssignedToUserId);

        return MapToDto(task);
    }

    public async Task<TaskResponseDto> CreateFromActivityAsync(
        int initiativeId,
        int activityId,
        int assignedToUserId,
        string title)
    {
        var caller = await GetCallerAsync();

        var task = new InitiativeTask
        {
            InitiativeId = initiativeId,
            Title = RequireTitle(title),
            AssignedToUserId = await ResolveAssigneeAsync(assignedToUserId),
            CreatedByUserId = caller.Id,
            DueDate = null,
            Priority = InitiativeTaskPriority.Medium,
            Status = InitiativeTaskStatus.NotStarted,
            SourceActivityId = activityId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        var created = await _taskRepository.AddAsync(task);

        await EnsureTeamMembershipAsync(initiativeId, created.AssignedToUserId);

        return MapToDto(created);
    }

    public async Task RemoveAsync(int initiativeId, int taskId)
    {
        var task = await RequireTaskAsync(initiativeId, taskId);

        await _taskRepository.RemoveAsync(task);
    }

    private async Task RequireInitiativeAsync(int initiativeId)
    {
        _ = await _initiativeRepository.GetByIdAsync(initiativeId)
            ?? throw new NotFoundException(
                $"Initiative {initiativeId} does not exist.");
    }

    /// <summary>
    /// Loads a task and confirms it belongs to the Initiative in the route, so a task on
    /// one Initiative cannot be edited or deleted through another's URL.
    /// </summary>
    private async Task<InitiativeTask> RequireTaskAsync(int initiativeId, int taskId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId)
            ?? throw new NotFoundException($"Task {taskId} does not exist.");

        if (task.InitiativeId != initiativeId)
        {
            throw new NotFoundException(
                $"Task {taskId} does not belong to Initiative {initiativeId}.");
        }

        return task;
    }

    /// <summary>
    /// Resolves the signed-in caller to their local user row, which CreatedByUserId
    /// points at. The row is created by GET /api/users/me on first sign-in.
    /// </summary>
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

    /// <summary>Null is valid — it leaves the task unassigned.</summary>
    private async Task<int?> ResolveAssigneeAsync(int? assignedToUserId)
    {
        if (assignedToUserId is null)
        {
            return null;
        }

        var assignee = await _userRepository.GetByIdAsync(assignedToUserId.Value)
            ?? throw new ValidationException(
                $"Assignee {assignedToUserId} does not exist.");

        if (!assignee.IsActive)
        {
            throw new ValidationException(
                $"{assignee.DisplayName} is deactivated and cannot be assigned work.");
        }

        return assignee.Id;
    }

    /// <summary>
    /// Puts the assignee on the Initiative's team if they are not already on it.
    /// </summary>
    /// <remarks>
    /// Assigning someone work makes them part of that work, so the team list should say
    /// so without anyone having to add them by hand. Runs after the task is stored, so a
    /// failure here cannot leave a member enrolled for a task that was never created.
    ///
    /// The unique index on (InitiativeId, UserId) is the real guarantee against
    /// duplicates; this check exists to avoid attempting an insert that would violate it.
    /// </remarks>
    private async Task EnsureTeamMembershipAsync(int initiativeId, int? userId)
    {
        if (userId is null)
        {
            return;
        }

        if (await _memberRepository.ExistsAsync(initiativeId, userId.Value))
        {
            return;
        }

        await _memberRepository.AddAsync(new InitiativeMember
        {
            InitiativeId = initiativeId,
            UserId = userId.Value,
            Role = DefaultAssigneeRole,
            ResponsibilityArea = null,
            Allocation = null,
            JoinedAt = DateTime.UtcNow
        });
    }

    private static string RequireTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Task title is required.");
        }

        return title.Trim();
    }

    /// <summary>
    /// Null is valid — a task need not have a deadline. A date before the floor is a
    /// typo or an omitted value that model binding turned into 0001-01-01.
    /// </summary>
    private static DateOnly? RequireDueDate(DateOnly? dueDate)
    {
        if (dueDate is null)
        {
            return null;
        }

        if (dueDate < InitiativeTask.EarliestDueDate)
        {
            throw new ValidationException(
                $"Due Date cannot be before {InitiativeTask.EarliestDueDate:yyyy-MM-dd}.");
        }

        return dueDate;
    }

    private static TaskResponseDto MapToDto(InitiativeTask task)
    {
        return new TaskResponseDto
        {
            Id = task.Id,
            InitiativeId = task.InitiativeId,
            Title = task.Title,
            AssignedToUserId = task.AssignedToUserId,
            AssignedToDisplayName = task.AssignedToUser?.DisplayName,
            CreatedByUserId = task.CreatedByUserId,
            CreatedByDisplayName = task.CreatedByUser?.DisplayName,
            DueDate = task.DueDate,
            Priority = task.Priority,
            Status = task.Status,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}
