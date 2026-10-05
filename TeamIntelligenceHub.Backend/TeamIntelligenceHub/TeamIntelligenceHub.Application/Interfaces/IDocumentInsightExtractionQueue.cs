namespace TeamIntelligenceHub.Application.Interfaces;

/// <summary>
/// Hands a Contribution attachment off for asynchronous testimonial/customer-story
/// extraction. Only produces the message; the separate extraction Function consumes it.
/// </summary>
public interface IDocumentInsightExtractionQueue
{
    /// <summary>
    /// Queues attachment for extraction. Best-effort: a failure here should not stop the
    /// attachment itself from being saved, so implementations are expected to swallow and
    /// log rather than throw.
    /// </summary>
    /// <param name="contributionAttachmentId">The attachment to extract insights from.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task EnqueueAsync(int contributionAttachmentId, CancellationToken cancellationToken = default);
}
