using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Application.Interfaces.Services;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Services;

public class ActivityService : IActivityService
{
    /// <summary>
    /// How much of the post becomes the title of an auto-created task. Long enough to
    /// be recognisable, short enough to read in a list.
    /// </summary>
    private const int TaskTitleLength = 80;

    private readonly IActivityRepository _activityRepository;
    private readonly IInitiativeRepository _initiativeRepository;
    private readonly IInitiativeTaskService _taskService;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public ActivityService(
        IActivityRepository activityRepository,
        IInitiativeRepository initiativeRepository,
        IInitiativeTaskService taskService,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _activityRepository = activityRepository;
        _initiativeRepository = initiativeRepository;
        _taskService = taskService;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<ActivityResponseDto>> GetByInitiativeAsync(int initiativeId)
    {
        await RequireInitiativeAsync(initiativeId);

        var activities = await _activityRepository.GetByInitiativeIdAsync(initiativeId);

        return activities
            .Select(MapToDto)
            .ToList();
    }

    public async Task<ActivityResponseDto> CreateAsync(
        int initiativeId,
        CreateActivityRequestDto request)
    {
        await RequireInitiativeAsync(initiativeId);

        var caller = await GetCallerAsync();
        var mentionedUserIds = await ResolveMentionsAsync(request.MentionedUserIds);

        var activity = new Activity
        {
            InitiativeId = initiativeId,
            // The author is whoever holds the token, never a value from the body.
            UserId = caller.Id,
            ActivityMessage = RequireMessage(request.ActivityMessage),
            AutoCreateTaskEnabled = request.AutoCreateTaskEnabled,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        var created = await _activityRepository.AddAsync(activity);

        await _activityRepository.ReplaceMentionsAsync(created.Id, mentionedUserIds);

        if (request.AutoCreateTaskEnabled)
        {
            await RaiseTasksAsync(
                initiativeId, created.Id, created.ActivityMessage, mentionedUserIds, caller.Id);
        }

        return await ReloadAsync(created.Id);
    }

    public async Task<ActivityResponseDto> UpdateAsync(
        int initiativeId,
        int activityId,
        UpdateActivityRequestDto request)
    {
        var activity = await RequireActivityAsync(initiativeId, activityId);
        var caller = await GetCallerAsync();

        if (activity.UserId != caller.Id)
        {
            throw new ValidationException("You can only edit your own updates.");
        }

        var mentionedUserIds = await ResolveMentionsAsync(request.MentionedUserIds);

        activity.ActivityMessage = RequireMessage(request.ActivityMessage);
        activity.UpdatedAt = DateTime.UtcNow;

        await _activityRepository.UpdateAsync(activity);

        await _activityRepository.ReplaceMentionsAsync(activity.Id, mentionedUserIds);

        // Auto-create deliberately does not run again. Tasks are raised once, when the
        // update is posted; re-running on every edit would duplicate work already
        // assigned, and possibly work somebody has since completed.
        return await ReloadAsync(activity.Id);
    }

    public async Task RemoveAsync(int initiativeId, int activityId)
    {
        var activity = await RequireActivityAsync(initiativeId, activityId);
        var caller = await GetCallerAsync();

        if (activity.UserId != caller.Id)
        {
            throw new ValidationException("You can only delete your own updates.");
        }

        // Tasks raised by this post survive it, detached. Removing assigned work
        // because the post that prompted it was deleted would lose real state.
        await _activityRepository.RemoveAsync(activity);
    }

    /// <summary>
    /// Raises one task per person mentioned.
    /// </summary>
    /// <remarks>
    /// The author is skipped: mentioning yourself in your own update is a reference,
    /// not an assignment. Mentions are already deduplicated, so nobody gets two.
    /// </remarks>
    private async Task RaiseTasksAsync(
        int initiativeId,
        int activityId,
        string message,
        IReadOnlyCollection<int> mentionedUserIds,
        int authorUserId)
    {
        var title = message.Length > TaskTitleLength
            ? message[..TaskTitleLength].TrimEnd()
            : message;

        foreach (var userId in mentionedUserIds.Where(id => id != authorUserId))
        {
            await _taskService.CreateFromActivityAsync(
                initiativeId, activityId, userId, title);
        }
    }

    private async Task RequireInitiativeAsync(int initiativeId)
    {
        _ = await _initiativeRepository.GetByIdAsync(initiativeId)
            ?? throw new NotFoundException(
                $"Initiative {initiativeId} does not exist.");
    }

    /// <summary>
    /// Loads a post and confirms it belongs to the Initiative in the route, so a post on
    /// one Initiative cannot be edited or deleted through another's URL.
    /// </summary>
    private async Task<Activity> RequireActivityAsync(int initiativeId, int activityId)
    {
        var activity = await _activityRepository.GetByIdAsync(activityId)
            ?? throw new NotFoundException($"Update {activityId} does not exist.");

        if (activity.InitiativeId != initiativeId)
        {
            throw new NotFoundException(
                $"Update {activityId} does not belong to Initiative {initiativeId}.");
        }

        return activity;
    }

    /// <summary>
    /// Collapses duplicates and rejects unknown people, so a mention always resolves to
    /// somebody who can be notified — and, with auto-create on, assigned work.
    /// </summary>
    private async Task<List<int>> ResolveMentionsAsync(List<int>? mentionedUserIds)
    {
        if (mentionedUserIds is null || mentionedUserIds.Count == 0)
        {
            return [];
        }

        var distinct = mentionedUserIds.Distinct().ToList();

        foreach (var userId in distinct)
        {
            _ = await _userRepository.GetByIdAsync(userId)
                ?? throw new ValidationException(
                    $"Mentioned user {userId} does not exist.");
        }

        return distinct;
    }

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

    private async Task<ActivityResponseDto> ReloadAsync(int activityId)
    {
        var activity = await _activityRepository.GetByIdAsync(activityId)
            ?? throw new NotFoundException($"Update {activityId} does not exist.");

        return MapToDto(activity);
    }

    private static string RequireMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ValidationException("An update is required.");
        }

        return message.Trim();
    }

    private static ActivityResponseDto MapToDto(Activity activity)
    {
        return new ActivityResponseDto
        {
            Id = activity.Id,
            InitiativeId = activity.InitiativeId,
            UserId = activity.UserId,
            UserDisplayName = activity.User?.DisplayName ?? string.Empty,
            ActivityMessage = activity.ActivityMessage,
            AutoCreateTaskEnabled = activity.AutoCreateTaskEnabled,
            CreatedAt = activity.CreatedAt,
            UpdatedAt = activity.UpdatedAt,
            Mentions = activity.Mentions
                .Select(m => new ActivityMentionDto
                {
                    MentionedUserId = m.MentionedUserId,
                    DisplayName = m.MentionedUser?.DisplayName ?? string.Empty
                })
                .ToList(),
            CreatedTasks = activity.CreatedTasks
                .Select(t => new ActivityCreatedTaskDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    AssignedToUserId = t.AssignedToUserId,
                    AssignedToDisplayName = t.AssignedToUser?.DisplayName
                })
                .ToList()
        };
    }
}
