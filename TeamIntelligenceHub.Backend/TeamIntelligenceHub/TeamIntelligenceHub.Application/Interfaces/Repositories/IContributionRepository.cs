using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

/// <summary>
/// Stores and loads contributions together with their credited people, links, and
/// detail sections.
/// </summary>
public interface IContributionRepository
{
    /// <summary>
    /// Loads one contribution with its whole graph.
    /// </summary>
    /// <param name="id">The contribution identifier.</param>
    /// <returns>The contribution, or null when it does not exist.</returns>
    Task<Contribution?> GetByIdAsync(int id);

    /// <summary>
    /// Returns the contribution feed for one Initiative, newest first.
    /// </summary>
    /// <param name="initiativeId">The Initiative whose feed to load.</param>
    /// <returns>The contributions on the feed.</returns>
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
    /// <returns>The submitted Customer Stories.</returns>
    Task<List<Contribution>> GetCustomerStoriesAsync();

    /// <summary>
    /// Every submitted Testimonial, across all Initiatives, newest first. Same filtering
    /// rationale as GetCustomerStoriesAsync.
    /// </summary>
    /// <returns>The submitted Testimonials.</returns>
    Task<List<Contribution>> GetTestimonialsAsync();

    /// <summary>
    /// Inserts the root contribution row.
    /// </summary>
    /// <param name="contribution">The contribution to insert.</param>
    /// <returns>The contribution with its generated id.</returns>
    Task<Contribution> AddAsync(Contribution contribution);

    /// <summary>
    /// Saves changes to the root contribution row.
    /// </summary>
    /// <param name="contribution">The contribution with its changes applied.</param>
    Task UpdateAsync(Contribution contribution);

    /// <summary>
    /// Deletes the contribution; its child rows cascade in the database.
    /// </summary>
    /// <param name="contribution">The contribution to delete.</param>
    Task RemoveAsync(Contribution contribution);

    /// <summary>
    /// Swaps the credited people for a new set.
    /// </summary>
    /// <remarks>
    /// Delete and reinsert rather than diff. The rows carry no history worth preserving,
    /// and the unique index on (ContributionId, UserId) makes a partial diff fiddlier
    /// than it is worth.
    /// </remarks>
    /// <param name="contributionId">The contribution whose credited people to replace.</param>
    /// <param name="contributors">The new set of credited people.</param>
    Task ReplaceContributorsAsync(
        int contributionId,
        IReadOnlyCollection<ContributionContributor> contributors);

    /// <summary>
    /// Swaps the links for a new set, on the same reasoning.
    /// </summary>
    /// <param name="contributionId">The contribution whose links to replace.</param>
    /// <param name="links">The new set of links.</param>
    Task ReplaceLinksAsync(
        int contributionId,
        IReadOnlyCollection<ContributionLink> links);

    /// <summary>
    /// Writes, replaces, or clears the five conditional detail rows in one pass. A null
    /// argument means "this section does not apply", and any existing row is removed.
    /// </summary>
    /// <param name="contributionId">The contribution the sections belong to.</param>
    /// <param name="metric">The metric section, or null to clear it.</param>
    /// <param name="risk">The risk section, or null to clear it.</param>
    /// <param name="aiPractice">The AI practice section, or null to clear it.</param>
    /// <param name="customerStory">The customer story section, or null to clear it.</param>
    /// <param name="testimonial">The testimonial section, or null to clear it.</param>
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
    /// column, which is a scan; fine at this scale, and it avoids a separate tag table.
    /// </remarks>
    /// <param name="search">Optional text the tags must contain.</param>
    /// <param name="take">The maximum number of tags to return.</param>
    /// <returns>The matching distinct tags.</returns>
    Task<List<string>> GetTagVocabularyAsync(string? search, int take);
}
