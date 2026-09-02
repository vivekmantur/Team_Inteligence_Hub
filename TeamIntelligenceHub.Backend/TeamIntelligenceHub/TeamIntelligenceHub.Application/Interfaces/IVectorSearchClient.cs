namespace TeamIntelligenceHub.Application.Interfaces;

/// <summary>
/// One chunk of indexed content returned by a similarity search, close enough to the
/// query vector to be worth grounding an answer on.
/// </summary>
public class RetrievedChunk
{
    public string Content { get; set; } = null!;

    public string? Title { get; set; }

    /// <summary>Where this chunk came from — a file path or URL, for citing back to the user.</summary>
    public string? Source { get; set; }

    /// <summary>The search engine's own relevance score, for logging and tie-breaking only.</summary>
    public double Score { get; set; }
}

/// <summary>
/// Finds indexed content near a query vector.
/// </summary>
/// <remarks>
/// Declared here so the Application layer can ground an answer without knowing Azure AI
/// Search is behind it. The query vector is computed elsewhere (<see cref="IEmbeddingClient"/>)
/// and handed in already embedded, so this interface never needs to know which embedding
/// model produced it. How many chunks come back is a search-tuning concern, so it is left
/// to the implementation's own configuration rather than passed in here.
/// </remarks>
public interface IVectorSearchClient
{
    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        ReadOnlyMemory<float> queryVector,
        CancellationToken cancellationToken = default);
}
