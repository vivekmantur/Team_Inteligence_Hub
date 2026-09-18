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

    /// <summary>Every submitted Customer Story, across all Initiatives, newest first.</summary>
    Task<List<CustomerStoryCardDto>> GetCustomerStoriesAsync();

    /// <summary>Every submitted Testimonial, across all Initiatives, newest first.</summary>
    Task<List<TestimonialCardDto>> GetTestimonialsAsync();

    /// <summary>
    /// Every customer story extracted from a Contribution attachment's document content,
    /// newest first, for the Stories &amp; Evidence page's "Extracted from documents"
    /// section.
    /// </summary>
    Task<List<DocumentCustomerStoryCardDto>> GetDocumentCustomerStoriesAsync();

    /// <summary>
    /// Every testimonial extracted from a Contribution attachment's document content,
    /// newest first, for the Stories &amp; Evidence page's "Extracted from documents"
    /// section.
    /// </summary>
    Task<List<DocumentTestimonialCardDto>> GetDocumentTestimonialsAsync();

    /// <summary>Distinct tags already in use, for the client's typeahead.</summary>
    Task<List<string>> GetTagVocabularyAsync(string? search, int? take);
}
