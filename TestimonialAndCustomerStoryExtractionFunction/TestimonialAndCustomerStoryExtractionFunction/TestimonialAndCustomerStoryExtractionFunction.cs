using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace TestimonialAndCustomerStoryExtractionFunction;

/// <summary>
/// Fires once per uploaded Contribution attachment. The API enqueues one message per
/// attachment, delayed long enough to cover Azure AI Search's own indexing lag, carrying
/// just the ContributionAttachmentId as plain text.
/// </summary>
public class TestimonialAndCustomerStoryExtractionFunction
{
    private readonly TestimonialAndCustomerStoryExtractor _extractor;
    private readonly ILogger<TestimonialAndCustomerStoryExtractionFunction> _logger;

    public TestimonialAndCustomerStoryExtractionFunction(
        TestimonialAndCustomerStoryExtractor extractor,
        ILogger<TestimonialAndCustomerStoryExtractionFunction> logger)
    {
        _extractor = extractor;
        _logger = logger;
    }

    [Function(nameof(TestimonialAndCustomerStoryExtractionFunction))]
    public async Task Run(
        [QueueTrigger("testimonial-customer-story-extraction", Connection = "StorageConnection")]
        string message,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(message, out var contributionAttachmentId))
        {
            // Malformed message — retrying it would never succeed, so log and drop it
            // rather than letting it exhaust its retries into the poison queue for
            // nothing.
            _logger.LogWarning(
                "Could not parse queue message {Message} as a ContributionAttachmentId. Skipping.",
                message);

            return;
        }

        await _extractor.ExtractAsync(contributionAttachmentId, cancellationToken);
    }
}
