// 1. Complete a chat from a system prompt and a user prompt

using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;

namespace TeamIntelligenceHub.Infrastructure.AI;

/// <summary>
/// Implements IChatCompletionClient by sending a system and user message to the configured
/// Azure OpenAI chat deployment.
/// </summary>
public class AzureOpenAiChatClient : IChatCompletionClient
{
    private readonly AzureOpenAiOptions _options;
    private readonly Lazy<ChatClient> _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureOpenAiChatClient"/> class.
    /// </summary>
    /// <param name="options">The Azure OpenAI settings that name the endpoint and chat deployment.</param>
    public AzureOpenAiChatClient(IOptions<AzureOpenAiOptions> options)
    {
        _options = options.Value;
        _client = new Lazy<ChatClient>(CreateClient);
    }

    /// <summary>Creates the chat client, using the API key when one is set and a managed identity otherwise.</summary>
    private ChatClient CreateClient()
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

        return azureClient.GetChatClient(_options.ChatDeploymentName);
    }

    /// <inheritdoc />
    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.Value.CompleteChatAsync(
                [
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userPrompt)
                ],
                cancellationToken: cancellationToken);

            return response.Value.Content.Count > 0
                ? response.Value.Content[0].Text
                : string.Empty;
        }
        catch (RequestFailedException ex)
        {
            throw new CopilotException(
                $"Could not generate an answer: {ex.ErrorCode ?? ex.Status.ToString()}. " +
                $"{ex.Message}",
                ex);
        }
    }
}
