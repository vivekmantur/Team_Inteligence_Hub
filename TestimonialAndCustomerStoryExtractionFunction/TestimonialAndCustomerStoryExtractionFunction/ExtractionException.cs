namespace TestimonialAndCustomerStoryExtractionFunction;

/// <summary>
/// Local, self-contained equivalent of the main backend's CopilotException — thrown when
/// an Azure OpenAI or Azure AI Search call fails or is misconfigured.
/// </summary>
public class ExtractionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractionException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the failure.</param>
    public ExtractionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractionException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the failure.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    public ExtractionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
