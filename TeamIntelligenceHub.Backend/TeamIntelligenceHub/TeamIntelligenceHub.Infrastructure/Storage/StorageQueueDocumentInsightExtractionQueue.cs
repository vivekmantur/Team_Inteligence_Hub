// 1. Queue a contribution attachment for testimonial and customer story extraction

using System.Text;
using Azure.Identity;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamIntelligenceHub.Application.Interfaces;

namespace TeamIntelligenceHub.Infrastructure.Storage;

/// <summary>
/// Sends a ContributionAttachment's id to the Storage Queue that
/// TestimonialAndCustomerStoryExtractionFunction listens on. It reuses BlobStorageOptions
/// because the queue lives in the same storage account.
/// </summary>
public class StorageQueueDocumentInsightExtractionQueue : IDocumentInsightExtractionQueue
{
    /// <summary>The queue that the extraction Function listens on.</summary>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageQueueDocumentInsightExtractionQueue"/> class.
    /// </summary>
    /// <param name="options">The blob storage settings whose storage account also holds the queue.</param>
    /// <param name="logger">The logger that records skipped and failed enqueues.</param>
    public StorageQueueDocumentInsightExtractionQueue(
        IOptions<BlobStorageOptions> options,
        ILogger<StorageQueueDocumentInsightExtractionQueue> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new Lazy<QueueClient>(
            CreateClient, LazyThreadSafetyMode.PublicationOnly);
    }

    /// <summary>Creates the queue client from the connection string or account URI and creates the queue when it does not exist yet.</summary>
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

    /// <inheritdoc />
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
