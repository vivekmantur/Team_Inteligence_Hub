using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

public interface IInitiativeService
{
    Task<InitiativeResponseDto?> GetByIdAsync(int id);

    Task<List<InitiativeResponseDto>> GetAllAsync();

    Task<InitiativeResponseDto> CreateAsync(CreateInitiativeRequestDto request);
}
