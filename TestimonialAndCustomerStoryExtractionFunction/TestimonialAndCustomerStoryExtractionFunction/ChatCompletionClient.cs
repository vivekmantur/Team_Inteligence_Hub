// 1. Send a system and user prompt and return the model's reply

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

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatCompletionClient"/> class.
    /// </summary>
    /// <param name="options">The Azure OpenAI settings used to create the chat client.</param>
    /// <param name="logger">The logger used to record requests, replies and failures.</param>
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

    /// <summary>
    /// Sends a system prompt and a user prompt to the chat model and returns the text of its reply.
    /// </summary>
    /// <param name="systemPrompt">The instructions that set the model's task and output format.</param>
    /// <param name="userPrompt">The user message, holding the content to analyze.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The text of the first reply content part, or an empty string when there is none.</returns>
    /// <exception cref="ExtractionException">
    /// Thrown when Azure OpenAI is not configured or the request fails.
    /// </exception>
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
