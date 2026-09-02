namespace TeamIntelligenceHub.Application.Interfaces;

/// <summary>
/// Turns text into the vector representation a similarity search can compare.
/// </summary>
/// <remarks>
/// Declared here so the Application layer can ask for an embedding without knowing Azure
/// OpenAI is behind it. Swapping the embedding model or provider touches only the
/// Infrastructure implementation.
/// </remarks>
public interface IEmbeddingClient
{
    Task<ReadOnlyMemory<float>> EmbedAsync(
        string text,
        CancellationToken cancellationToken = default);
}
