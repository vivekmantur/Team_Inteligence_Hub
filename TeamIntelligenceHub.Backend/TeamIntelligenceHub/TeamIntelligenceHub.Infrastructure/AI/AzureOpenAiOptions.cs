namespace TeamIntelligenceHub.Infrastructure.AI;

/// <summary>
/// Binds the "AzureOpenAI" configuration section.
/// </summary>
/// <remarks>
/// Values resolve through IConfiguration, so appsettings.json, user secrets, and
/// environment variables all work without a code change. Supply an ApiKey, or leave it
/// blank to authenticate with a managed identity instead via DefaultAzureCredential.
/// </remarks>
public class AzureOpenAiOptions
{
    public const string SectionName = "AzureOpenAI";

    /// <summary>e.g. https://myresource.openai.azure.com/</summary>
    public string? Endpoint { get; set; }

    /// <summary>Prefer leaving this blank and using a managed identity instead.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Deployment name of the chat model, e.g. "gpt-4o".</summary>
    public string? ChatDeploymentName { get; set; }

    /// <summary>Deployment name of the embedding model, e.g. "text-embedding-3-large".</summary>
    public string? EmbeddingDeploymentName { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint) &&
        !string.IsNullOrWhiteSpace(ChatDeploymentName) &&
        !string.IsNullOrWhiteSpace(EmbeddingDeploymentName);
}
