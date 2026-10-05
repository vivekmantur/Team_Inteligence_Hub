// 1. Get a contribution attachment by ID
// 2. Add a contribution attachment
// 3. Remove a contribution attachment

using Microsoft.EntityFrameworkCore;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IContributionAttachmentRepository for attachment metadata rows.
/// </summary>
public class ContributionAttachmentRepository : IContributionAttachmentRepository
{
    private readonly TeamIntelligenceHubDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContributionAttachmentRepository"/> class.
    /// </summary>
    /// <param name="context">The database context used to read and save contribution attachment rows.</param>
    public ContributionAttachmentRepository(TeamIntelligenceHubDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ContributionAttachment?> GetByIdAsync(int id)
    {
        // The parent comes along so the service can check the attachment really belongs
        // to the contribution named in the route.
        return await _context.ContributionAttachments
            .Include(x => x.Contribution)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<ContributionAttachment> AddAsync(ContributionAttachment attachment)
    {
        await _context.ContributionAttachments.AddAsync(attachment);

        await _context.SaveChangesAsync();

        return attachment;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(ContributionAttachment attachment)
    {
        _context.ContributionAttachments.Remove(attachment);

        await _context.SaveChangesAsync();
    }
}
