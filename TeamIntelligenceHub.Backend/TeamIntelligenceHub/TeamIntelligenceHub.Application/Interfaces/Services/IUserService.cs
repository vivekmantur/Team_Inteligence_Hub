using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

public interface IUserService
{
    Task<UserResponseDto?> GetByIdAsync(int id);

    Task<List<UserResponseDto>> GetAllAsync();

    Task<UserResponseDto> GetCurrentUserAsync();
}