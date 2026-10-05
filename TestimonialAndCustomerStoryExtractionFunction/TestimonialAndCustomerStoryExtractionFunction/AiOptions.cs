namespace TestimonialAndCustomerStoryExtractionFunction;

/// <summary>
/// Local redeclaration of the main backend's AzureOpenAiOptions. Binds the same
/// "AzureOpenAI" configuration section — same section name, so the values already in
/// local.settings.json (copied from the main API's own config) work without renaming
/// anything.
/// </summary>
public class AzureOpenAiOptions
{
    /// <summary>The configuration section these options bind to.</summary>
    public const string SectionName = "AzureOpenAI";

    /// <summary>Gets or sets the Azure OpenAI resource endpoint URL.</summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the API key for the Azure OpenAI resource. Prefer leaving this blank and
    /// using a managed identity instead.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>Gets or sets the deployment name of the chat completion model.</summary>
    public string? ChatDeploymentName { get; set; }

    /// <summary>Gets or sets the deployment name of the embedding model.</summary>
    public string? EmbeddingDeploymentName { get; set; }

    /// <summary>Gets a value indicating whether the endpoint and both deployment names are set.</summary>
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
    /// <summary>The configuration section these options bind to.</summary>
    public const string SectionName = "AzureAiSearch";

    /// <summary>Gets or sets the Azure AI Search service endpoint URL.</summary>
    public string? Endpoint { get; set; }

    /// <summary>Gets or sets the API key for the search service, or null to use a managed identity.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Gets or sets the name of the index to search.</summary>
    public string? IndexName { get; set; }

    /// <summary>Gets or sets the index field that holds each chunk's text.</summary>
    public string ContentField { get; set; } = "chunk";

    /// <summary>Gets or sets the index field that holds each chunk's embedding vector.</summary>
    public string VectorField { get; set; } = "text_vector";

    /// <summary>Gets or sets the index field that holds the source document title, if any.</summary>
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
    /// "https://&lt;account&gt;.blob.core.windows.net/contribution-attachments" (no
    /// trailing slash). Required for scoped search: combined with a
    /// ContributionAttachment's BlobName and Base64URL(no padding)-encoded, this
    /// reproduces the prefix of the value SourceField actually stores per chunk.
    /// </summary>
    public string? SourceUrlPrefix { get; set; }

    /// <summary>Gets or sets the number of chunks a search returns.</summary>
    public int TopNDocuments { get; set; } = 5;

    /// <summary>Gets a value indicating whether the endpoint and index name are set.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(IndexName);
}
