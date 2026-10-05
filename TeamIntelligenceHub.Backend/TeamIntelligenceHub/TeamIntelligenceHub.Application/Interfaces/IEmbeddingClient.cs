namespace TeamIntelligenceHub.Application.Interfaces;

/// <summary>
/// Turns text into the vector representation a similarity search can compare, without
/// the Application layer knowing which embedding provider is behind it.
/// </summary>
public interface IEmbeddingClient
{
    /// <summary>
    /// Returns the embedding vector for the given text.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The embedding vector.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.CopilotException">
    /// Thrown when the embedding call fails.
    /// </exception>
    Task<ReadOnlyMemory<float>> EmbedAsync(
        string text,
        CancellationToken cancellationToken = default);
}
