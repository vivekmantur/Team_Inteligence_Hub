using Microsoft.EntityFrameworkCore;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Domain.Entities;
using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Repositories;

public class DocumentTestimonialAndCustomerStoryRepository
    : IDocumentTestimonialAndCustomerStoryRepository
{
    private readonly TeamIntelligenceHubDbContext _context;

    public DocumentTestimonialAndCustomerStoryRepository(TeamIntelligenceHubDbContext context)
    {
        _context = context;
    }

    public async Task<DocumentTestimonialAndCustomerStory> AddAsync(
        DocumentTestimonialAndCustomerStory row)
    {
        await _context.DocumentTestimonialsAndCustomerStories.AddAsync(row);

        await _context.SaveChangesAsync();

        return row;
    }

    public async Task<List<DocumentTestimonialAndCustomerStory>> GetByTypeAsync(
        DocumentInsightType type)
    {
        return await _context.DocumentTestimonialsAndCustomerStories
            .Include(x => x.ContributionAttachment)
                .ThenInclude(a => a.Contribution)
                    .ThenInclude(c => c.Initiative)
            .Where(x => x.Type == type
                && x.ContributionAttachment.Contribution.Status == ContributionStatus.Submitted)
            .OrderByDescending(x => x.Id)
            .ToListAsync();
    }
}
