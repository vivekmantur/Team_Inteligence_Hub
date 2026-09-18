using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

public interface IContributionRepository
{
    /// <summary>Loads one contribution with its whole graph.</summary>
    Task<Contribution?> GetByIdAsync(int id);

    /// <summary>The feed for one Initiative, newest first.</summary>
    Task<List<Contribution>> GetByInitiativeIdAsync(int initiativeId);

    /// <summary>
    /// Every submitted Customer Story, across all Initiatives, newest first.
    /// </summary>
    /// <remarks>
    /// Filters on the presence of the CustomerStory detail row rather than the Types
    /// JSON column: RequireSectionsMatchTypes guarantees the two agree, and a row
    /// existence check is the simpler query to translate. Drafts are excluded — this
    /// feeds a company-wide showcase, not a personal feed, so a work-in-progress story
    /// should not be visible outside its own Initiative yet.
    /// </remarks>
    Task<List<Contribution>> GetCustomerStoriesAsync();

    /// <summary>
    /// Every submitted Testimonial, across all Initiatives, newest first. Same filtering
    /// rationale as GetCustomerStoriesAsync.
    /// </summary>
    Task<List<Contribution>> GetTestimonialsAsync();

    Task<Contribution> AddAsync(Contribution contribution);

    Task UpdateAsync(Contribution contribution);

    Task RemoveAsync(Contribution contribution);

    /// <summary>
    /// Swaps the credited people for a new set.
    /// </summary>
    /// <remarks>
    /// Delete and reinsert rather than diff. The rows carry no history worth preserving,
    /// and the unique index on (ContributionId, UserId) makes a partial diff fiddlier
    /// than it is worth.
    /// </remarks>
    Task ReplaceContributorsAsync(
        int contributionId,
        IReadOnlyCollection<ContributionContributor> contributors);

    /// <summary>Swaps the links for a new set, on the same reasoning.</summary>
    Task ReplaceLinksAsync(
        int contributionId,
        IReadOnlyCollection<ContributionLink> links);

    /// <summary>
    /// Writes, replaces, or clears the five conditional detail rows in one pass. A null
    /// argument means "this section does not apply", and any existing row is removed.
    /// </summary>
    Task SaveDetailSectionsAsync(
        int contributionId,
        ContributionMetric? metric,
        ContributionRisk? risk,
        ContributionAiPractice? aiPractice,
        ContributionCustomerStory? customerStory,
        ContributionTestimonial? testimonial);

    /// <summary>
    /// The distinct tag vocabulary, for the client's typeahead.
    /// </summary>
    /// <remarks>
    /// Without this every author invents their own spelling and the tag column stops
    /// being useful for filtering. Translates to CROSS APPLY OPENJSON over the Tags
    /// column, which is a scan; fine at this scale, and the alternative was a table.
    /// </remarks>
    Task<List<string>> GetTagVocabularyAsync(string? search, int take);
}
