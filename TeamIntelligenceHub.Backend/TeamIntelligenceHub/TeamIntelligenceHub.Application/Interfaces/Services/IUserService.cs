using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// Reads users and keeps the signed-in caller's local row in step with Entra.
/// </summary>
public interface IUserService
{
    /// <summary>Returns one user, or null when it does not exist.</summary>
    Task<UserResponseDto?> GetByIdAsync(int id);

    /// <summary>Returns every user, each with their allocation summed across Initiatives.</summary>
    Task<List<UserResponseDto>> GetAllAsync();

    /// <summary>
    /// Returns the caller's local user row, creating it on first sign-in and refreshing
    /// email and display name from the token otherwise. Throws UnauthorizedAccessException
    /// when the token is missing or carries unusable identity claims.
    /// </summary>
    Task<UserResponseDto> GetCurrentUserAsync();

    /// <summary>
    /// Sets a user's AppRole. Only the signed-in user may edit their own — anyone else's
    /// id is rejected, regardless of who asks.
    /// </summary>
    Task<UserResponseDto> UpdateAppRoleAsync(int userId, UpdateAppRoleRequestDto request);
}
