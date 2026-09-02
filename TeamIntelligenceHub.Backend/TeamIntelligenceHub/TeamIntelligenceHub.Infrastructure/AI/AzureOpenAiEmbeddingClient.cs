using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;

namespace TeamIntelligenceHub.Infrastructure.AI;

public class AzureOpenAiEmbeddingClient : IEmbeddingClient
{
    private readonly AzureOpenAiOptions _options;
    private readonly Lazy<EmbeddingClient> _client;

    public AzureOpenAiEmbeddingClient(IOptions<AzureOpenAiOptions> options)
    {
        _options = options.Value;
        _client = new Lazy<EmbeddingClient>(CreateClient);
    }

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
