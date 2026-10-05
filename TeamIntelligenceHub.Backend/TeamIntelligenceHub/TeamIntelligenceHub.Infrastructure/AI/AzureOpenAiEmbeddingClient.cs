// 1. Generate the embedding vector for a text

using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;

namespace TeamIntelligenceHub.Infrastructure.AI;

/// <summary>
/// Implements IEmbeddingClient by generating a text's embedding vector with the configured
/// Azure OpenAI embedding deployment.
/// </summary>
public class AzureOpenAiEmbeddingClient : IEmbeddingClient
{
    private readonly AzureOpenAiOptions _options;
    private readonly Lazy<EmbeddingClient> _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureOpenAiEmbeddingClient"/> class.
    /// </summary>
    /// <param name="options">The Azure OpenAI settings that name the endpoint and embedding deployment.</param>
    public AzureOpenAiEmbeddingClient(IOptions<AzureOpenAiOptions> options)
    {
        _options = options.Value;
        _client = new Lazy<EmbeddingClient>(CreateClient);
    }

    /// <summary>Creates the embedding client, using the API key when one is set and a managed identity otherwise.</summary>
    private EmbeddingClient CreateClient()
    {
        if (!_options.IsConfigured)
        {
            throw new CopilotException(
                "Azure OpenAI is not configured. Set AzureOpenAI:Endpoint, " +
                "AzureOpenAI:ChatDeploymentName, and AzureOpenAI:EmbeddingDeploymentName " +
                "(and AzureOpenAI:ApiKey, unless using a managed identity).");
        }

        var endpoint = new Uri(_options.Endpoint!);

        var azureClient = string.IsNullOrWhiteSpace(_options.ApiKey)
            ? new AzureOpenAIClient(endpoint, new DefaultAzureCredential())
            : new AzureOpenAIClient(endpoint, new AzureKeyCredential(_options.ApiKey));

        return azureClient.GetEmbeddingClient(_options.EmbeddingDeploymentName);
    }

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<float>> EmbedAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.Value.GenerateEmbeddingAsync(
                text, cancellationToken: cancellationToken);

            return response.Value.ToFloats();
        }
        catch (RequestFailedException ex)
        {
            throw new CopilotException(
                $"Could not generate an embedding: {ex.ErrorCode ?? ex.Status.ToString()}. " +
                $"{ex.Message}",
                ex);
        }
    }
}
