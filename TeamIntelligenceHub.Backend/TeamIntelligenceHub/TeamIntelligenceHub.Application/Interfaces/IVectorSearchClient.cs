namespace TeamIntelligenceHub.Application.Interfaces;

/// <summary>
/// One chunk of indexed content returned by a similarity search, close enough to the
/// query vector to be worth grounding an answer on.
/// </summary>
public class RetrievedChunk
{
    /// <summary>The chunk's text.</summary>
    public string Content { get; set; } = null!;

    /// <summary>The title of the document the chunk belongs to, when the index has one.</summary>
    public string? Title { get; set; }

    /// <summary>Where this chunk came from — a file path or URL, for citing back to the user.</summary>
    public string? Source { get; set; }

    /// <summary>The search engine's own relevance score, for logging and tie-breaking only.</summary>
    public double Score { get; set; }
}

/// <summary>
/// Finds indexed content near a query vector, which <see cref="IEmbeddingClient"/>
/// computes beforehand. How many chunks come back is set by the implementation's configuration.
/// </summary>
public interface IVectorSearchClient
{
    /// <summary>
    /// Returns the indexed chunks nearest the query vector.
    /// </summary>
    /// <param name="queryVector">The embedded query to search near.</param>
    /// <param name="searchText">
    /// When supplied, the implementation runs a hybrid search — keyword relevance (BM25)
    /// fused with vector relevance — instead of vector search alone. Null runs a
    /// vector-only search.
    /// </param>
    /// <param name="sourceBlobName">
    /// When supplied, restricts the search to the single document with this name/path,
    /// instead of the whole index. What "restrict" means concretely (which index field to
    /// filter on) is an implementation detail the caller does not need to know.
    /// </param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The nearest chunks, most relevant first.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.CopilotException">
    /// Thrown when the search call fails.
    /// </exception>
    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        ReadOnlyMemory<float> queryVector,
        string? searchText = null,
        string? sourceBlobName = null,
        CancellationToken cancellationToken = default);
}
