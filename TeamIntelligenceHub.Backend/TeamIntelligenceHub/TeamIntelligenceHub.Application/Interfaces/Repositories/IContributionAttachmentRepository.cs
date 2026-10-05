using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

/// <summary>
/// Stores and loads the metadata rows for files attached to contributions.
/// </summary>
public interface IContributionAttachmentRepository
{
    /// <summary>
    /// Loads one attachment row.
    /// </summary>
    /// <param name="id">The attachment identifier.</param>
    /// <returns>The attachment row, or null when it does not exist.</returns>
    Task<ContributionAttachment?> GetByIdAsync(int id);

    /// <summary>
    /// Inserts a new attachment row.
    /// </summary>
    /// <param name="attachment">The attachment row to insert.</param>
    /// <returns>The attachment row with its generated id.</returns>
    Task<ContributionAttachment> AddAsync(ContributionAttachment attachment);

    /// <summary>
    /// Deletes the attachment row. The stored file is not touched.
    /// </summary>
    /// <param name="attachment">The attachment row to delete.</param>
    Task RemoveAsync(ContributionAttachment attachment);
}
