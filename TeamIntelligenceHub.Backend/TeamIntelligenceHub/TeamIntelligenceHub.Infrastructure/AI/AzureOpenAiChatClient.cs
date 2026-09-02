using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;

namespace TeamIntelligenceHub.Infrastructure.AI;

public class AzureOpenAiChatClient : IChatCompletionClient
{
    private readonly AzureOpenAiOptions _options;
    private readonly Lazy<ChatClient> _client;

    public AzureOpenAiChatClient(IOptions<AzureOpenAiOptions> options)
    {
        _options = options.Value;
        _client = new Lazy<ChatClient>(CreateClient);
    }

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
