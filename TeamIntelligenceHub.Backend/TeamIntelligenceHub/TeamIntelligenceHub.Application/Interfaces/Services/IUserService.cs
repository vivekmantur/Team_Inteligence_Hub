using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// Reads users and keeps the signed-in caller's local row in step with Entra.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Returns one user, with their allocation summed across Initiatives.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <returns>The user, or null when it does not exist.</returns>
    Task<UserResponseDto?> GetByIdAsync(int id);

    /// <summary>
    /// Returns every user, each with their allocation summed across Initiatives.
    /// </summary>
    /// <returns>All users.</returns>
    Task<List<UserResponseDto>> GetAllAsync();

    /// <summary>
    /// Returns the caller's local user row, creating it on first sign-in and refreshing
    /// email and display name from the token otherwise.
    /// </summary>
    /// <returns>The caller's user row.</returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the token is missing or carries unusable identity claims.
    /// </exception>
    Task<UserResponseDto> GetCurrentUserAsync();

    /// <summary>
    /// Sets a user's AppRole. Only the signed-in user may edit their own — anyone else's
    /// id is rejected, regardless of who asks.
    /// </summary>
    /// <param name="userId">The user whose AppRole to set; must be the caller.</param>
    /// <param name="request">The new AppRole.</param>
    /// <returns>The updated user.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown when the user is not the caller or the caller has no profile.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the token carries no Entra object id.
    /// </exception>
    Task<UserResponseDto> UpdateAppRoleAsync(int userId, UpdateAppRoleRequestDto request);
}
