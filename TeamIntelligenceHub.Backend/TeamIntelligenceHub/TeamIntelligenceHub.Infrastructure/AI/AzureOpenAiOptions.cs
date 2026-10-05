namespace TeamIntelligenceHub.Infrastructure.AI;

/// <summary>
/// Binds the "AzureOpenAI" configuration section. Leave ApiKey blank to authenticate
/// with a managed identity through DefaultAzureCredential.
/// </summary>
public class AzureOpenAiOptions
{
    /// <summary>The name of the configuration section these options bind to.</summary>
    public const string SectionName = "AzureOpenAI";

    /// <summary>The Azure OpenAI resource endpoint, e.g. https://myresource.openai.azure.com/.</summary>
    public string? Endpoint { get; set; }

    /// <summary>The API key for the resource. Prefer leaving this blank and using a managed identity instead.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Deployment name of the chat model, e.g. "gpt-4o".</summary>
    public string? ChatDeploymentName { get; set; }

    /// <summary>Deployment name of the embedding model, e.g. "text-embedding-3-large".</summary>
    public string? EmbeddingDeploymentName { get; set; }

    /// <summary>Gets a value indicating whether the endpoint and both deployment names are set.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint) &&
        !string.IsNullOrWhiteSpace(ChatDeploymentName) &&
        !string.IsNullOrWhiteSpace(EmbeddingDeploymentName);
}
