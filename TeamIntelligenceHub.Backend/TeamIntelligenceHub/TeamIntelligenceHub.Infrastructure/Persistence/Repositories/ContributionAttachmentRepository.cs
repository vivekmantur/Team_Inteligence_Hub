using Microsoft.EntityFrameworkCore;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Repositories;

public class ContributionAttachmentRepository : IContributionAttachmentRepository
{
    private readonly TeamIntelligenceHubDbContext _context;

    public ContributionAttachmentRepository(TeamIntelligenceHubDbContext context)
    {
        _context = context;
    }

    public async Task<ContributionAttachment?> GetByIdAsync(int id)
    {
        // The parent comes along so the service can check the attachment really belongs
        // to the contribution named in the route.
        return await _context.ContributionAttachments
            .Include(x => x.Contribution)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<ContributionAttachment> AddAsync(ContributionAttachment attachment)
    {
        await _context.ContributionAttachments.AddAsync(attachment);

        await _context.SaveChangesAsync();

        return attachment;
    }

    public async Task RemoveAsync(ContributionAttachment attachment)
    {
        _context.ContributionAttachments.Remove(attachment);

        await _context.SaveChangesAsync();
    }
}
