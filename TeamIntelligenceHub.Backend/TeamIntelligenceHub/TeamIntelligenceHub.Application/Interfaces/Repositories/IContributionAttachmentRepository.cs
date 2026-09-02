using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

public interface IContributionAttachmentRepository
{
    Task<ContributionAttachment?> GetByIdAsync(int id);

    Task<ContributionAttachment> AddAsync(ContributionAttachment attachment);

    Task RemoveAsync(ContributionAttachment attachment);
}
