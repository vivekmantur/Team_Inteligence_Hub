using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// Reads and writes contributions and the showcase cards built from them.
/// </summary>
public interface IContributionService
{
    /// <summary>
    /// Returns every contribution on the Initiative. Throws NotFoundException when the
    /// Initiative does not exist.
    /// </summary>
    Task<List<ContributionResponseDto>> GetByInitiativeAsync(int initiativeId);

    /// <summary>Returns one contribution. Throws NotFoundException when it does not exist.</summary>
    Task<ContributionResponseDto> GetByIdAsync(int contributionId);

    /// <summary>
    /// Creates a contribution submitted by the caller, with its whole graph, and enrolls
    /// every credited person on the Initiative's team. Throws NotFoundException for an
    /// unknown Initiative and ValidationException for invalid input.
    /// </summary>
    Task<ContributionResponseDto> CreateAsync(
        int initiativeId,
        CreateContributionRequestDto request);

    /// <summary>
    /// Replaces the whole graph of a contribution the caller submitted. Throws
    /// NotFoundException for an unknown contribution and ValidationException for someone
    /// else's contribution or invalid input.
    /// </summary>
    Task<ContributionResponseDto> UpdateAsync(
        int contributionId,
        UpdateContributionRequestDto request);

    /// <summary>
    /// Deletes a contribution the caller submitted, along with its stored files. Throws
    /// NotFoundException for an unknown contribution and ValidationException for someone
    /// else's.
    /// </summary>
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
