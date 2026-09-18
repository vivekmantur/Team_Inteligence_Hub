using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Application.Interfaces.Services;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IInitiativeMemberRepository _memberRepository;
    private readonly ICurrentUserService _currentUserService;

    public UserService(
        IUserRepository userRepository,
        IInitiativeMemberRepository memberRepository,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _memberRepository = memberRepository;
        _currentUserService = currentUserService;
    }

    public async Task<UserResponseDto?> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null)
        {
            return null;
        }

        return MapToDto(user, await GetTotalAllocationAsync(id));
    }

    public async Task<List<UserResponseDto>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync();

        var totalAllocationByUserId = await _memberRepository
            .GetTotalAllocationByUserIdsAsync(users.Select(u => u.Id).Distinct().ToList());

        return users
            .Select(u => MapToDto(u, totalAllocationByUserId.GetValueOrDefault(u.Id)))
            .ToList();
    }

    public async Task<UserResponseDto> GetCurrentUserAsync()
    {
        if (!_currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException(
                "User is not authenticated.");
        }

        var entraObjectId = _currentUserService.EntraObjectId;

        if (string.IsNullOrWhiteSpace(entraObjectId))
        {
            throw new UnauthorizedAccessException(
                "Entra Object ID was not found.");
        }

        if (entraObjectId.Length > User.EntraObjectIdMaxLength)
        {
            throw new UnauthorizedAccessException(
                "Entra Object ID is longer than the column allows.");
        }

        var email = _currentUserService.Email;

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UnauthorizedAccessException(
                "Email claim was not found on the token.");
        }

        email = email.Trim();

        // Claims are external input. A value that cannot be stored is a hard failure,
        // because silently truncating an address would corrupt the identity record.
        if (email.Length > User.EmailMaxLength)
        {
            throw new UnauthorizedAccessException(
                "Email claim is longer than the column allows.");
        }

        if (!email.Contains('@') || email.StartsWith('@') || email.EndsWith('@'))
        {
            throw new UnauthorizedAccessException(
                "Email claim is not a valid address.");
        }

        // Email stands in for a missing name so the required column never
        // silently stores a blank.
        var displayName = string.IsNullOrWhiteSpace(
            _currentUserService.DisplayName)
                ? email
                : _currentUserService.DisplayName!.Trim();

        // A display name is cosmetic, so trim it to fit rather than locking the
        // person out of the application over it.
        if (displayName.Length > User.DisplayNameMaxLength)
        {
            displayName = displayName[..User.DisplayNameMaxLength];
        }

        var user = await _userRepository
            .GetByEntraObjectIdAsync(entraObjectId);

        if (user == null)
        {
            user = new User
            {
                EntraObjectId = entraObjectId,
                Email = email,
                DisplayName = displayName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            user = await _userRepository.AddAsync(user);
        }
        else
        {
            // Names and addresses change in Entra; keep the local row in step.
            user.Email = email;
            user.DisplayName = displayName;
            user.LastLoginAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
        }

        return MapToDto(user, await GetTotalAllocationAsync(user.Id));
    }

    public async Task<UserResponseDto> UpdateAppRoleAsync(int userId, UpdateAppRoleRequestDto request)
    {
        var entraObjectId = _currentUserService.EntraObjectId;

        if (string.IsNullOrWhiteSpace(entraObjectId))
        {
            throw new UnauthorizedAccessException("Entra Object ID was not found.");
        }

        var caller = await _userRepository.GetByEntraObjectIdAsync(entraObjectId)
            ?? throw new ValidationException(
                "Your profile has not been created yet. Reload the app and try again.");

        // Not just "no one else can rename you" — this is the only path that writes
        // AppRole at all, so it doubles as the entire access rule for the feature.
        if (caller.Id != userId)
        {
            throw new ValidationException("You can only edit your own role.");
        }

        caller.AppRole = request.AppRole.Trim();

        await _userRepository.UpdateAsync(caller);

        return MapToDto(caller, await GetTotalAllocationAsync(caller.Id));
    }

    private async Task<decimal> GetTotalAllocationAsync(int userId)
    {
        var totals = await _memberRepository.GetTotalAllocationByUserIdsAsync([userId]);

        return totals.GetValueOrDefault(userId);
    }

    private static UserResponseDto MapToDto(
        User user, decimal totalAllocationAcrossInitiatives)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            EntraObjectId = user.EntraObjectId,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AppRole = user.AppRole,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            TotalAllocationAcrossInitiatives = totalAllocationAcrossInitiatives
        };
    }
}
