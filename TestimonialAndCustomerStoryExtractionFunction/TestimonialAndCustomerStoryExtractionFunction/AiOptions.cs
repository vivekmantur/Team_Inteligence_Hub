namespace TestimonialAndCustomerStoryExtractionFunction;

/// <summary>
/// Local redeclaration of the main backend's AzureOpenAiOptions. Binds the same
/// "AzureOpenAI" configuration section — same section name, so the values already in
/// local.settings.json (copied from the main API's own config) work without renaming
/// anything.
/// </summary>
public class AzureOpenAiOptions
{
    public const string SectionName = "AzureOpenAI";

    public string? Endpoint { get; set; }

    /// <summary>Prefer leaving this blank and using a managed identity instead.</summary>
    public string? ApiKey { get; set; }

    public string? ChatDeploymentName { get; set; }

    public string? EmbeddingDeploymentName { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint) &&
        !string.IsNullOrWhiteSpace(ChatDeploymentName) &&
        !string.IsNullOrWhiteSpace(EmbeddingDeploymentName);
}

/// <summary>
/// Local redeclaration of the main backend's AzureAiSearchOptions. Binds the same
/// "AzureAiSearch" configuration section.
/// </summary>
public class AzureAiSearchOptions
{
    public const string SectionName = "AzureAiSearch";

    public string? Endpoint { get; set; }

    public string? ApiKey { get; set; }

    public string? IndexName { get; set; }

    public string ContentField { get; set; } = "chunk";

    public string VectorField { get; set; } = "text_vector";

    public string? TitleField { get; set; } = "title";

    /// <summary>
    /// Field a scoped search matches against — the id field the "Import and vectorize
    /// data" wizard assigns per text chunk, e.g. "text_document_id". NOT "content_path":
    /// that field is null for text chunks and, where populated for image chunks, holds an
    /// unrelated derived-image render path. Does not need to be filterable: matching is
    /// done client-side (a StartsWith against the encoded key from SourceUrlPrefix +
    /// the blob name), not via a server-side $filter, because the stored value carries an
    /// undocumented page-index suffix after the encoded URL that an exact match would miss.
    /// </summary>
    public string? SourceField { get; set; }

    /// <summary>
    /// The blob container's base URL — e.g.
    /// "https://tihstorage2026.blob.core.windows.net/contribution-attachments" (no
    /// trailing slash). Required for scoped search: combined with a
    /// ContributionAttachment's BlobName and Base64URL(no padding)-encoded, this
    /// reproduces the prefix of the value SourceField actually stores per chunk.
    /// </summary>
    public string? SourceUrlPrefix { get; set; }

    public int TopNDocuments { get; set; } = 5;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(IndexName);
}
