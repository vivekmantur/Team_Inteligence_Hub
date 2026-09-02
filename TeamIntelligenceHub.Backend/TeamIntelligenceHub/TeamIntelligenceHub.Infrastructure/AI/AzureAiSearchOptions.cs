namespace TeamIntelligenceHub.Infrastructure.AI;

/// <summary>
/// Binds the "AzureAiSearch" configuration section.
/// </summary>
/// <remarks>
/// Values resolve through IConfiguration, so appsettings.json, user secrets, and
/// environment variables all work without a code change. Supply an ApiKey, or leave it
/// blank to authenticate with a managed identity instead via DefaultAzureCredential.
///
/// The field names default to what the "Import and vectorize data" wizard in the Azure
/// portal names them. Override the ones that differ in appsettings.json — these are index
/// schema, not secrets, so they belong in source control rather than user secrets.
/// </remarks>
public class AzureAiSearchOptions
{
    public const string SectionName = "AzureAiSearch";

    /// <summary>e.g. https://myservice.search.windows.net</summary>
    public string? Endpoint { get; set; }

    /// <summary>Prefer leaving this blank and using a managed identity instead.</summary>
    public string? ApiKey { get; set; }

    public string? IndexName { get; set; }

    /// <summary>Field holding the chunk's text.</summary>
    public string ContentField { get; set; } = "chunk";

    /// <summary>Field holding the chunk's embedding vector.</summary>
    public string VectorField { get; set; } = "text_vector";

    /// <summary>Field holding a human-readable title, if the index has one.</summary>
    public string? TitleField { get; set; } = "title";

    /// <summary>
    /// Field holding the source file path or URL, if the index has one. Null by default —
    /// unlike ContentField/VectorField/TitleField, indexes built without the blob indexer's
    /// metadata fields commonly have nothing here (e.g. just chunk_id/parent_id/chunk/
    /// title/text_vector), so guessing a name would break the $select clause outright
    /// instead of degrading to "no source shown".
    /// </summary>
    public string? SourceField { get; set; }

    /// <summary>How many chunks to retrieve per question.</summary>
    public int TopNDocuments { get; set; } = 5;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(IndexName);
}
