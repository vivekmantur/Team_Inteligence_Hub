using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

public interface IUserService
{
    Task<UserResponseDto?> GetByIdAsync(int id);

    Task<List<UserResponseDto>> GetAllAsync();

    Task<UserResponseDto> GetCurrentUserAsync();

    /// <summary>
    /// Sets a user's AppRole. Only the signed-in user may edit their own — anyone else's
    /// id is rejected, regardless of who asks.
    /// </summary>
    Task<UserResponseDto> UpdateAppRoleAsync(int userId, UpdateAppRoleRequestDto request);
}