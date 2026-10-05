using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

/// <summary>
/// Stores and loads the metadata rows for files attached to contributions.
/// </summary>
public interface IContributionAttachmentRepository
{
    /// <summary>Loads one attachment row, or null when it does not exist.</summary>
    Task<ContributionAttachment?> GetByIdAsync(int id);

    /// <summary>Inserts a new attachment row and returns it with its generated id.</summary>
    Task<ContributionAttachment> AddAsync(ContributionAttachment attachment);

    /// <summary>Deletes the attachment row. The stored file is not touched.</summary>
    Task RemoveAsync(ContributionAttachment attachment);
}
