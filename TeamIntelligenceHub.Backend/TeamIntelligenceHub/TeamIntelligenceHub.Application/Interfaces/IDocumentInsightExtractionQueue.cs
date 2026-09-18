namespace TeamIntelligenceHub.Application.Interfaces;

/// <summary>
/// Hands a Contribution attachment off for asynchronous testimonial/customer-story
/// extraction.
/// </summary>
/// <remarks>
/// Declared here so the Application layer can trigger extraction without knowing an Azure
/// Storage Queue is behind it. The consumer is the standalone
/// TestimonialAndCustomerStoryExtractionFunction project — this interface only produces
/// the message; nothing about how it's processed lives in this codebase.
/// </remarks>
public interface IDocumentInsightExtractionQueue
{
    /// <summary>
    /// Queues attachment for extraction. Best-effort: a failure here should not stop the
    /// attachment itself from being saved, so implementations are expected to swallow and
    /// log rather than throw.
    /// </summary>
    Task EnqueueAsync(int contributionAttachmentId, CancellationToken cancellationToken = default);
}
