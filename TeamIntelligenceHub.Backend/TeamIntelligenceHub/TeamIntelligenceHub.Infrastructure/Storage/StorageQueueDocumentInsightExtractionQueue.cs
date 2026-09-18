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
    /// Azure AI Search only picks up a newly uploaded blob when its indexer next runs —
    /// on a 30-minute timer schedule here — and there is no event or webhook the app can
    /// wait on instead; the indexer is entirely out-of-band and portal-managed. Without
    /// this delay the Function would dequeue and search immediately, before the document
    /// exists in the index at all, and find nothing. 35 minutes covers one full 30-minute
    /// schedule interval (worst case: the blob lands the instant after a run starts) plus
    /// a few minutes' buffer for the indexer itself to finish processing it. If the
    /// indexer's schedule changes, this should change with it.
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
