using TeamIntelligenceHub.Domain.Entities;
using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Application.Interfaces.Repositories;

/// <summary>
/// Stores and loads testimonials and customer stories extracted from contribution
/// attachments.
/// </summary>
public interface IDocumentTestimonialAndCustomerStoryRepository
{
    /// <summary>Inserts one extracted row and returns it with its generated id.</summary>
    Task<DocumentTestimonialAndCustomerStory> AddAsync(
        DocumentTestimonialAndCustomerStory row);

    /// <summary>
    /// Every extracted row of one insight type, newest first, for a Contribution that was
    /// actually submitted. Includes the source attachment, its Contribution, and that
    /// Contribution's Initiative, so the caller can build a card without further queries.
    /// </summary>
    Task<List<DocumentTestimonialAndCustomerStory>> GetByTypeAsync(DocumentInsightType type);
}
