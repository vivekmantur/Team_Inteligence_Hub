using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace TestimonialAndCustomerStoryExtractionFunction;

/// <summary>
/// Local, self-contained equivalent of the main backend's AzureOpenAiChatClient — same
/// SDK, same call shape, no shared code.
/// </summary>
public class ChatCompletionClient
{
    private readonly AzureOpenAiOptions _options;
    private readonly Lazy<OpenAI.Chat.ChatClient> _client;
    private readonly ILogger<ChatCompletionClient> _logger;

    public ChatCompletionClient(
        IOptions<AzureOpenAiOptions> options, ILogger<ChatCompletionClient> logger)
    {
        _options = options.Value;
        _client = new Lazy<OpenAI.Chat.ChatClient>(CreateClient);
        _logger = logger;
    }

    private OpenAI.Chat.ChatClient CreateClient()
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
            "Creating chat client for endpoint {Endpoint}, deployment {Deployment}, auth={Auth}.",
            endpoint, _options.ChatDeploymentName,
            string.IsNullOrWhiteSpace(_options.ApiKey) ? "DefaultAzureCredential" : "ApiKey");

        var azureClient = string.IsNullOrWhiteSpace(_options.ApiKey)
            ? new AzureOpenAIClient(endpoint, new DefaultAzureCredential())
            : new AzureOpenAIClient(endpoint, new AzureKeyCredential(_options.ApiKey));

        return azureClient.GetChatClient(_options.ChatDeploymentName);
    }

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending chat completion request. System prompt length={SystemLength}, " +
            "user prompt length={UserLength}. User prompt preview: \"{Preview}\"",
            systemPrompt.Length, userPrompt.Length, Preview(userPrompt));

        try
        {
            var response = await _client.Value.CompleteChatAsync(
                [
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userPrompt)
                ],
                cancellationToken: cancellationToken);

            var text = response.Value.Content.Count > 0
                ? response.Value.Content[0].Text
                : string.Empty;

            _logger.LogInformation("Chat completion response: \"{Response}\"", text);

            return text;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                ex, "Chat completion request failed: {ErrorCode} {Message}",
                ex.ErrorCode ?? ex.Status.ToString(), ex.Message);

            throw new ExtractionException(
                $"Could not generate a completion: {ex.ErrorCode ?? ex.Status.ToString()}. " +
                $"{ex.Message}",
                ex);
        }
    }

    private static string Preview(string text) =>
        text.Length <= 200 ? text : text[..200] + "...";
}
