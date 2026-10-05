namespace TeamIntelligenceHub.Application.Interfaces;

/// <summary>
/// Generates a natural-language answer from a prompt.
/// </summary>
/// <remarks>
/// Declared here so the Application layer can ask an LLM for an answer without knowing
/// Azure OpenAI is behind it.
/// </remarks>
public interface IChatCompletionClient
{
    /// <summary>Returns the model's answer to the user prompt, under the rules the system prompt sets.</summary>
    Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
