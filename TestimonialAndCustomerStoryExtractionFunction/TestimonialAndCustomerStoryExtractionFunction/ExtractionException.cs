namespace TestimonialAndCustomerStoryExtractionFunction;

/// <summary>
/// Local, self-contained equivalent of the main backend's CopilotException — thrown when
/// an Azure OpenAI or Azure AI Search call fails or is misconfigured.
/// </summary>
public class ExtractionException : Exception
{
    public ExtractionException(string message) : base(message)
    {
    }

    public ExtractionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
