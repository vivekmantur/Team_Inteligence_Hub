namespace TeamIntelligenceHub.Application.Exceptions;

/// <summary>
/// Raised when a step of answering a Copilot question fails — the embedding call, the
/// search call, or the chat completion call. Wraps the provider's exception so the API
/// layer needs no Azure SDK dependency.
/// </summary>
public class CopilotException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotException"/> class.
    /// </summary>
    /// <param name="message">The reason the Copilot step failed.</param>
    /// <param name="innerException">The provider exception that caused the failure, if any.</param>
    public CopilotException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
