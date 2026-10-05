using TeamIntelligenceHub.Application.DTOs;

namespace TeamIntelligenceHub.Application.Interfaces.Services;

/// <summary>
/// Reads and writes contributions and the showcase cards built from them.
/// </summary>
public interface IContributionService
{
    /// <summary>
    /// Returns every contribution on the Initiative.
    /// </summary>
    /// <param name="initiativeId">The Initiative whose contributions to load.</param>
    /// <returns>The contributions on the Initiative.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the Initiative does not exist.
    /// </exception>
    Task<List<ContributionResponseDto>> GetByInitiativeAsync(int initiativeId);

    /// <summary>
    /// Returns one contribution.
    /// </summary>
    /// <param name="contributionId">The contribution identifier.</param>
    /// <returns>The contribution with its whole graph.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the contribution does not exist.
    /// </exception>
    Task<ContributionResponseDto> GetByIdAsync(int contributionId);

    /// <summary>
    /// Creates a contribution submitted by the caller, with its whole graph, and enrolls
    /// every credited person on the Initiative's team.
    /// </summary>
    /// <param name="initiativeId">The Initiative the contribution belongs to.</param>
    /// <param name="request">The contribution, its credited people, links, and detail sections.</param>
    /// <returns>The created contribution.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the Initiative does not exist.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown for invalid input or a caller with no profile.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the token carries no Entra object id.
    /// </exception>
    Task<ContributionResponseDto> CreateAsync(
        int initiativeId,
        CreateContributionRequestDto request);

    /// <summary>
    /// Replaces the whole graph of a contribution the caller submitted.
    /// </summary>
    /// <param name="contributionId">The contribution to update.</param>
    /// <param name="request">The replacement contribution graph.</param>
    /// <returns>The updated contribution.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the contribution does not exist.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown for someone else's contribution or invalid input.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the token carries no Entra object id.
    /// </exception>
    Task<ContributionResponseDto> UpdateAsync(
        int contributionId,
        UpdateContributionRequestDto request);

    /// <summary>
    /// Deletes a contribution the caller submitted, along with its stored files.
    /// </summary>
    /// <param name="contributionId">The contribution to delete.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.NotFoundException">
    /// Thrown when the contribution does not exist.
    /// </exception>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.ValidationException">
    /// Thrown for someone else's contribution.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the token carries no Entra object id.
    /// </exception>
    Task RemoveAsync(int contributionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every submitted Customer Story, across all Initiatives, newest first.
    /// </summary>
    /// <returns>The Customer Story cards.</returns>
    Task<List<CustomerStoryCardDto>> GetCustomerStoriesAsync();

    /// <summary>
    /// Returns every submitted Testimonial, across all Initiatives, newest first.
    /// </summary>
    /// <returns>The Testimonial cards.</returns>
    Task<List<TestimonialCardDto>> GetTestimonialsAsync();

    /// <summary>
    /// Returns every customer story extracted from a Contribution attachment's document
    /// content, newest first, for the Stories &amp; Evidence page's "Extracted from
    /// documents" section.
    /// </summary>
    /// <returns>The extracted customer story cards.</returns>
    Task<List<DocumentCustomerStoryCardDto>> GetDocumentCustomerStoriesAsync();

    /// <summary>
    /// Returns every testimonial extracted from a Contribution attachment's document
    /// content, newest first, for the Stories &amp; Evidence page's "Extracted from
    /// documents" section.
    /// </summary>
    /// <returns>The extracted testimonial cards.</returns>
    Task<List<DocumentTestimonialCardDto>> GetDocumentTestimonialsAsync();

    /// <summary>
    /// Returns the distinct tags already in use, for the client's typeahead.
    /// </summary>
    /// <param name="search">Optional text the tags must contain.</param>
    /// <param name="take">The maximum number of tags to return; clamped to a safe range, with a default when null.</param>
    /// <returns>The matching tags.</returns>
    Task<List<string>> GetTagVocabularyAsync(string? search, int? take);
}
