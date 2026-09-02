using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;

namespace TeamIntelligenceHub.Infrastructure.AI;

public class AzureAiSearchVectorClient : IVectorSearchClient
{
    private readonly AzureAiSearchOptions _options;
    private readonly Lazy<SearchClient> _client;

    public AzureAiSearchVectorClient(IOptions<AzureAiSearchOptions> options)
    {
        _options = options.Value;
        _client = new Lazy<SearchClient>(CreateClient);
    }

    private SearchClient CreateClient()
    {
        if (!_options.IsConfigured)
        {
            throw new CopilotException(
                "Azure AI Search is not configured. Set AzureAiSearch:Endpoint and " +
                "AzureAiSearch:IndexName (and AzureAiSearch:ApiKey, unless using a " +
                "managed identity).");
        }

        var endpoint = new Uri(_options.Endpoint!);

        return string.IsNullOrWhiteSpace(_options.ApiKey)
            ? new SearchClient(endpoint, _options.IndexName, new DefaultAzureCredential())
            : new SearchClient(
                endpoint, _options.IndexName, new AzureKeyCredential(_options.ApiKey));
    }

    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        ReadOnlyMemory<float> queryVector,
        CancellationToken cancellationToken = default)
    {
        var select = new List<string> { _options.ContentField };

        if (!string.IsNullOrWhiteSpace(_options.TitleField))
        {
            select.Add(_options.TitleField);
        }

        if (!string.IsNullOrWhiteSpace(_options.SourceField))
        {
            select.Add(_options.SourceField);
        }

        var searchOptions = new SearchOptions
        {
            Size = _options.TopNDocuments,
            VectorSearch = new()
            {
                Queries =
                {
                    new VectorizedQuery(queryVector)
                    {
                        KNearestNeighborsCount = _options.TopNDocuments,
                        Fields = { _options.VectorField }
                    }
                }
            }
        };

        foreach (var field in select)
        {
            searchOptions.Select.Add(field);
        }

        try
        {
            var response = await _client.Value.SearchAsync<SearchDocument>(
                searchText: null, searchOptions, cancellationToken);

            var chunks = new List<RetrievedChunk>();

            await foreach (var result in response.Value.GetResultsAsync())
            {
                chunks.Add(new RetrievedChunk
                {
                    Content = result.Document.TryGetValue(
                        _options.ContentField, out var content)
                        ? content?.ToString() ?? string.Empty
                        : string.Empty,
                    Title = _options.TitleField is not null
                        && result.Document.TryGetValue(_options.TitleField, out var title)
                        ? title?.ToString()
                        : null,
                    Source = _options.SourceField is not null
                        && result.Document.TryGetValue(_options.SourceField, out var source)
                        ? source?.ToString()
                        : null,
                    Score = result.Score ?? 0
                });
            }

            return chunks;
        }
        catch (RequestFailedException ex)
        {
            throw new CopilotException(
                $"Could not search the index: {ex.ErrorCode ?? ex.Status.ToString()}. " +
                $"{ex.Message}",
                ex);
        }
    }
}
