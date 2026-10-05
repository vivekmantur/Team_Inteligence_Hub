using System.Text;
using Azure.Identity;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamIntelligenceHub.Application.Interfaces;

namespace TeamIntelligenceHub.Infrastructure.Storage;

/// <summary>
/// Sends a ContributionAttachment's id to the Storage Queue that
/// TestimonialAndCustomerStoryExtractionFunction listens on.
/// </summary>
/// <remarks>
/// Reuses BlobStorageOptions' connection settings rather than a separate option class:
/// the queue lives in the same storage account as the blob containers, so there is only
/// one account to point at. The queue name is fixed rather than configurable because it
/// is a wiring detail shared with the Function's own [QueueTrigger] attribute, not
/// something that varies per environment.
/// </remarks>
public class StorageQueueDocumentInsightExtractionQueue : IDocumentInsightExtractionQueue
{
    private const string QueueName = "testimonial-customer-story-extraction";

    /// <summary>
    /// How long a message stays invisible before the Function can dequeue it.
    /// </summary>
    /// <remarks>
    /// Azure AI Search indexes a new blob only when its portal-managed indexer next runs,
    /// every 30 minutes here, and the app has no event to wait on. Without this delay the
    /// Function would search before the document is indexed and find nothing. 35 minutes
    /// covers one full interval plus a few minutes for the indexer to finish. Keep it in
    /// step with the indexer's schedule.
    /// </remarks>
    private static readonly TimeSpan IndexingLagVisibilityDelay = TimeSpan.FromMinutes(35);

    private readonly BlobStorageOptions _options;
    private readonly ILogger<StorageQueueDocumentInsightExtractionQueue> _logger;
    private readonly Lazy<QueueClient> _client;

    public StorageQueueDocumentInsightExtractionQueue(
        IOptions<BlobStorageOptions> options,
        ILogger<StorageQueueDocumentInsightExtractionQueue> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new Lazy<QueueClient>(
            CreateClient, LazyThreadSafetyMode.PublicationOnly);
    }

    private QueueClient CreateClient()
    {
        var client = !string.IsNullOrWhiteSpace(_options.ConnectionString)
            ? new QueueClient(_options.ConnectionString, QueueName)
            : new QueueClient(
                new Uri($"{_options.AccountUri!.TrimEnd('/')}/{QueueName}"),
                new DefaultAzureCredential());

        client.CreateIfNotExists();

        return client;
    }

    public async Task EnqueueAsync(
        int contributionAttachmentId, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogWarning(
                "Skipped queuing attachment {AttachmentId} for extraction: blob storage " +
                "is not configured, so the queue's storage account is unknown.",
                contributionAttachmentId);

            return;
        }

        try
        {
            // Functions' queue trigger expects Base64 and decodes it automatically.
            var message = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(contributionAttachmentId.ToString()));

            await _client.Value.SendMessageAsync(
                message,
                visibilityTimeout: IndexingLagVisibilityDelay,
                timeToLive: null,
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Extraction is a nice-to-have enrichment, not something the attachment
            // upload should fail over. Worst case, this document is never scanned.
            _logger.LogWarning(
                ex,
                "Could not queue attachment {AttachmentId} for extraction.",
                contributionAttachmentId);
        }
    }
}
