namespace TeamIntelligenceHub.Application.Interfaces;

/// <summary>
/// Generates a natural-language answer from a prompt, without the Application layer
/// knowing which LLM provider is behind it.
/// </summary>
public interface IChatCompletionClient
{
    /// <summary>
    /// Returns the model's answer to the user prompt, under the rules the system prompt sets.
    /// </summary>
    /// <param name="systemPrompt">The instructions that set the model's rules and tone.</param>
    /// <param name="userPrompt">The question or request the model answers.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The model's answer text.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.CopilotException">
    /// Thrown when the chat completion call fails.
    /// </exception>
    Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
