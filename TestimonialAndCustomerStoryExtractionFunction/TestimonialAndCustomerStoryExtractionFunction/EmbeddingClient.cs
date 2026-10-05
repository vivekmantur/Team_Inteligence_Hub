// 1. Generate an embedding vector for a piece of text

using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace TestimonialAndCustomerStoryExtractionFunction;

/// <summary>
/// Local, self-contained equivalent of the main backend's AzureOpenAiEmbeddingClient —
/// same SDK, same call shape, no shared code.
/// </summary>
public class EmbeddingClient
{
    private readonly AzureOpenAiOptions _options;
    private readonly Lazy<OpenAI.Embeddings.EmbeddingClient> _client;
    private readonly ILogger<EmbeddingClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddingClient"/> class.
    /// </summary>
    /// <param name="options">The Azure OpenAI settings used to create the embedding client.</param>
    /// <param name="logger">The logger used to record requests, responses and failures.</param>
    public EmbeddingClient(IOptions<AzureOpenAiOptions> options, ILogger<EmbeddingClient> logger)
    {
        _options = options.Value;
        _client = new Lazy<OpenAI.Embeddings.EmbeddingClient>(CreateClient);
        _logger = logger;
    }

    private OpenAI.Embeddings.EmbeddingClient CreateClient()
    {
        if (!_options.IsConfigured)
        {
            throw new ExtractionException(
                "Azure OpenAI is not configured. Set AzureOpenAI:Endpoint, " +
                "AzureOpenAI:ChatDeploymentName, and AzureOpenAI:EmbeddingDeploymentName " +
                "(and AzureOpenAI:ApiKey, unless using a managed identity).");
        }

        var endpoint = new Uri(_options.Endpoint!);

        _logger.LogInformation(
            "Creating embedding client for endpoint {Endpoint}, deployment {Deployment}, auth={Auth}.",
            endpoint, _options.EmbeddingDeploymentName,
            string.IsNullOrWhiteSpace(_options.ApiKey) ? "DefaultAzureCredential" : "ApiKey");

        var azureClient = string.IsNullOrWhiteSpace(_options.ApiKey)
            ? new AzureOpenAIClient(endpoint, new DefaultAzureCredential())
            : new AzureOpenAIClient(endpoint, new AzureKeyCredential(_options.ApiKey));

        return azureClient.GetEmbeddingClient(_options.EmbeddingDeploymentName);
    }

    /// <summary>
    /// Generates an embedding vector for the given text with the configured embedding model.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The embedding vector for the text.</returns>
    /// <exception cref="ExtractionException">
    /// Thrown when Azure OpenAI is not configured or the request fails.
    /// </exception>
    public async Task<ReadOnlyMemory<float>> EmbedAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Requesting embedding for text of length {Length}: \"{Preview}\"",
            text.Length, Preview(text));

        try
        {
            var response = await _client.Value.GenerateEmbeddingAsync(
                text, cancellationToken: cancellationToken);

            var vector = response.Value.ToFloats();

            _logger.LogInformation(
                "Embedding response received: {Dimensions} dimensions.", vector.Length);

            return vector;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                ex, "Embedding request failed: {ErrorCode} {Message}",
                ex.ErrorCode ?? ex.Status.ToString(), ex.Message);

            throw new ExtractionException(
                $"Could not generate an embedding: {ex.ErrorCode ?? ex.Status.ToString()}. " +
                $"{ex.Message}",
                ex);
        }
    }

    private static string Preview(string text) =>
        text.Length <= 120 ? text : text[..120] + "...";
}
