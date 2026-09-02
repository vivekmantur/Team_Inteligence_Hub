namespace TeamIntelligenceHub.Application.Exceptions;

/// <summary>
/// Raised when a step of answering a Copilot question fails — the embedding call, the
/// search call, or the chat completion call.
/// </summary>
/// <remarks>
/// Wraps the provider's own exception type so the API layer can report a useful reason
/// without taking a dependency on the Azure SDKs behind it.
/// </remarks>
public class CopilotException : Exception
{
    public CopilotException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
