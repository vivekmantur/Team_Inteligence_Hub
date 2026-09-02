using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

public interface IContributionService
{
    Task<List<ContributionResponseDto>> GetByInitiativeAsync(int initiativeId);

    Task<ContributionResponseDto> GetByIdAsync(int contributionId);

    Task<ContributionResponseDto> CreateAsync(
        int initiativeId,
        CreateContributionRequestDto request);

    Task<ContributionResponseDto> UpdateAsync(
        int contributionId,
        UpdateContributionRequestDto request);

    Task RemoveAsync(int contributionId, CancellationToken cancellationToken = default);

    /// <summary>Distinct tags already in use, for the client's typeahead.</summary>
    Task<List<string>> GetTagVocabularyAsync(string? search, int? take);
}
