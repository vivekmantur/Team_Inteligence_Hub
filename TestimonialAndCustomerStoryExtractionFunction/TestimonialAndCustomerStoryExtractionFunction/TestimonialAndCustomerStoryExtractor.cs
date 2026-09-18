using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TestimonialAndCustomerStoryExtractionFunction;

/// <summary>
/// Looks for a genuine testimonial and/or customer story in one Contribution attachment's
/// indexed content, and persists whatever is found.
/// </summary>
/// <remarks>
/// Fully self-contained: the extraction workflow, the data access, and the AI clients are
/// all local to this Function project — nothing is shared with the main backend.
/// </remarks>
public class TestimonialAndCustomerStoryExtractor
{
    private const string CustomerStoryQueryText =
        "A customer story describing a problem, solution, and measurable outcome or " +
        "business impact for a customer.";

    private const string TestimonialQueryText =
        "A direct quote or testimonial from a customer or stakeholder expressing " +
        "feedback, satisfaction, or an opinion about the product or team.";

    private readonly ExtractionDbContext _dbContext;
    private readonly EmbeddingClient _embeddingClient;
    private readonly DocumentSearchClient _searchClient;
    private readonly ChatCompletionClient _chatClient;
    private readonly ILogger<TestimonialAndCustomerStoryExtractor> _logger;

    public TestimonialAndCustomerStoryExtractor(
        ExtractionDbContext dbContext,
        EmbeddingClient embeddingClient,
        DocumentSearchClient searchClient,
        ChatCompletionClient chatClient,
        ILogger<TestimonialAndCustomerStoryExtractor> logger)
    {
        _dbContext = dbContext;
        _embeddingClient = embeddingClient;
        _searchClient = searchClient;
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task ExtractAsync(
        int contributionAttachmentId, CancellationToken cancellationToken = default)
    {
        var attachment = await _dbContext.ContributionAttachments
            .FirstOrDefaultAsync(x => x.Id == contributionAttachmentId, cancellationToken);

        _logger.LogInformation(
            "Starting extraction for attachment {AttachmentId}. Found={Found}, BlobName={BlobName}",
            contributionAttachmentId, attachment is not null, attachment?.BlobName);

        if (attachment is null)
        {
            // Deleted since this was queued. Nothing to extract, and not a failure —
            // the queue message should not be retried over this.
            _logger.LogInformation(
                "ContributionAttachment {AttachmentId} no longer exists. Skipping.",
                contributionAttachmentId);

            return;
        }

        // Storage Queues only guarantee at-least-once delivery, and a message can also be
        // redelivered locally if a debugger pause outlasts the lease's auto-renewal — so
        // this same attachment can genuinely be extracted more than once. Each type is
        // capped at one row per attachment by design (FindBestChunkAsync only ever
        // considers the single best-matching chunk), so a prior row for a type is proof
        // this attachment was already handled for it; skip re-running the search and LLM
        // call entirely rather than paying for them again and inserting a duplicate.
        var alreadyExtractedTypes = await _dbContext.DocumentTestimonialsAndCustomerStories
            .Where(x => x.ContributionAttachmentId == attachment.Id)
            .Select(x => x.Type)
            .ToListAsync(cancellationToken);

        var foundCustomerStory = alreadyExtractedTypes.Contains(DocumentInsightType.CustomerStory)
            ? LogAlreadyExtracted(attachment.Id, DocumentInsightType.CustomerStory)
            : await ExtractCustomerStoryAsync(attachment, cancellationToken);

        var foundTestimonial = alreadyExtractedTypes.Contains(DocumentInsightType.Testimonial)
            ? LogAlreadyExtracted(attachment.Id, DocumentInsightType.Testimonial)
            : await ExtractTestimonialAsync(attachment, cancellationToken);

        _logger.LogInformation(
            "Extraction complete for attachment {AttachmentId}: customer story={FoundCustomerStory}, testimonial={FoundTestimonial}.",
            attachment.Id, foundCustomerStory, foundTestimonial);
    }

    private bool LogAlreadyExtracted(int attachmentId, DocumentInsightType type)
    {
        _logger.LogInformation(
            "Skipping {Type} extraction for attachment {AttachmentId}: already extracted.",
            type, attachmentId);

        return true;
    }

    private async Task<bool> ExtractCustomerStoryAsync(
        ContributionAttachmentRecord attachment, CancellationToken cancellationToken)
    {
        var chunk = await FindBestChunkAsync(
            CustomerStoryQueryText, attachment.BlobName, cancellationToken);

        _logger.LogInformation(
            "Customer story chunk search for attachment {AttachmentId}: found={Found}",
            attachment.Id, chunk is not null);

        if (chunk is null)
        {
            return false;
        }

        var prompt = ExtractionPromptBuilder.BuildCustomerStoryPrompt(chunk.Content);

        var response = await _chatClient.CompleteAsync(
            prompt.SystemPrompt, prompt.UserPrompt, cancellationToken);

        var extracted = ExtractionResultParser.ParseCustomerStory(response);

        _logger.LogInformation(
            "Customer story parse result for attachment {AttachmentId}: extracted={Extracted}",
            attachment.Id, extracted is not null);

        if (extracted is null)
        {
            return false;
        }

        _dbContext.DocumentTestimonialsAndCustomerStories.Add(new DocumentTestimonialAndCustomerStory
        {
            ContributionAttachmentId = attachment.Id,
            Type = DocumentInsightType.CustomerStory,
            CustomerName = extracted.CustomerName,
            Summary = extracted.Summary,
            Outcome = extracted.Outcome,
            Quote = extracted.Quote,
            BusinessValue = extracted.BusinessValue
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<bool> ExtractTestimonialAsync(
        ContributionAttachmentRecord attachment, CancellationToken cancellationToken)
    {
        var chunk = await FindBestChunkAsync(
            TestimonialQueryText, attachment.BlobName, cancellationToken);

        _logger.LogInformation(
            "Testimonial chunk search for attachment {AttachmentId}: found={Found}",
            attachment.Id, chunk is not null);

        if (chunk is null)
        {
            return false;
        }

        var prompt = ExtractionPromptBuilder.BuildTestimonialPrompt(chunk.Content);

        var response = await _chatClient.CompleteAsync(
            prompt.SystemPrompt, prompt.UserPrompt, cancellationToken);

        var extracted = ExtractionResultParser.ParseTestimonial(response);

        _logger.LogInformation(
            "Testimonial parse result for attachment {AttachmentId}: extracted={Extracted}",
            attachment.Id, extracted is not null);

        if (extracted is null)
        {
            return false;
        }

        _dbContext.DocumentTestimonialsAndCustomerStories.Add(new DocumentTestimonialAndCustomerStory
        {
            ContributionAttachmentId = attachment.Id,
            Type = DocumentInsightType.Testimonial,
            Quote = extracted.Quote,
            SpeakerName = extracted.SpeakerName,
            SpeakerRole = extracted.SpeakerRole,
            Audience = extracted.Audience,
            Sentiment = extracted.Sentiment
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Embeds the query, then searches restricted to this one attachment's blob via a
    /// hybrid (keyword + vector) query — so paraphrased content is found, not just exact
    /// wording. The top result is the model's one chance to say whether it is genuine;
    /// there is no numeric score cutoff here by design, since a search-relevance score is
    /// not a reliable proxy for "is this actually a testimonial" — that judgment is left
    /// entirely to the extraction prompt's own NO_INSIGHT_FOUND escape hatch.
    /// </summary>
    private async Task<RetrievedChunk?> FindBestChunkAsync(
        string queryText, string blobName, CancellationToken cancellationToken)
    {
        var queryVector = await _embeddingClient.EmbedAsync(queryText, cancellationToken);

        var chunks = await _searchClient.SearchAsync(
            queryVector,
            searchText: queryText,
            sourceBlobName: blobName,
            cancellationToken: cancellationToken);

        return chunks.Count > 0 ? chunks[0] : null;
    }
}
