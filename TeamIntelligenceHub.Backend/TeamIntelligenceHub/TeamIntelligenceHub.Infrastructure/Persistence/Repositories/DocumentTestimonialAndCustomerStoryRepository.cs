// 1. Add an extracted testimonial or customer story
// 2. Get the extracted rows of one type from submitted contributions

using Microsoft.EntityFrameworkCore;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Domain.Entities;
using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IDocumentTestimonialAndCustomerStoryRepository for rows
/// extracted from attachments of submitted contributions.
/// </summary>
public class DocumentTestimonialAndCustomerStoryRepository
    : IDocumentTestimonialAndCustomerStoryRepository
{
    private readonly TeamIntelligenceHubDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentTestimonialAndCustomerStoryRepository"/> class.
    /// </summary>
    /// <param name="context">The database context used to read and save extracted testimonial and customer story rows.</param>
    public DocumentTestimonialAndCustomerStoryRepository(TeamIntelligenceHubDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<DocumentTestimonialAndCustomerStory> AddAsync(
        DocumentTestimonialAndCustomerStory row)
    {
        await _context.DocumentTestimonialsAndCustomerStories.AddAsync(row);

        await _context.SaveChangesAsync();

        return row;
    }

    /// <inheritdoc />
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
