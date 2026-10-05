namespace TeamIntelligenceHub.Infrastructure.AI;

/// <summary>
/// Binds the "AzureAiSearch" configuration section. Leave ApiKey blank to authenticate
/// with a managed identity through DefaultAzureCredential.
/// </summary>
public class AzureAiSearchOptions
{
    /// <summary>The name of the configuration section these options bind to.</summary>
    public const string SectionName = "AzureAiSearch";

    /// <summary>The search service endpoint, e.g. https://myservice.search.windows.net.</summary>
    public string? Endpoint { get; set; }

    /// <summary>The API key for the search service. Prefer leaving this blank and using a managed identity instead.</summary>
    public string? ApiKey { get; set; }

    /// <summary>The name of the index to query.</summary>
    public string? IndexName { get; set; }

    /// <summary>Field holding the chunk's text.</summary>
    public string ContentField { get; set; } = "chunk";

    /// <summary>Field holding the chunk's embedding vector.</summary>
    public string VectorField { get; set; } = "text_vector";

    /// <summary>Field holding a human-readable title, if the index has one.</summary>
    public string? TitleField { get; set; } = "title";

    /// <summary>
    /// Field holding the source file path or URL, if the index has one. Null by default:
    /// indexes built without the blob indexer's metadata fields often lack one, and a
    /// guessed name would break the $select clause instead of just showing no source.
    /// </summary>
    public string? SourceField { get; set; }

    /// <summary>How many chunks to retrieve per question.</summary>
    public int TopNDocuments { get; set; } = 5;

    /// <summary>Gets a value indicating whether the endpoint and index name are both set.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(IndexName);
}
