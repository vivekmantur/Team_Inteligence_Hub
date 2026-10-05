// 1. Search the index for the chunks closest to a query vector

using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;

namespace TeamIntelligenceHub.Infrastructure.AI;

/// <summary>
/// Implements IVectorSearchClient by running a vector query, with optional search text, against
/// the configured Azure AI Search index, optionally filtered to one source document.
/// </summary>
public class AzureAiSearchVectorClient : IVectorSearchClient
{
    private readonly AzureAiSearchOptions _options;
    private readonly Lazy<SearchClient> _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAiSearchVectorClient"/> class.
    /// </summary>
    /// <param name="options">The Azure AI Search settings that name the endpoint, index, and fields to query.</param>
    public AzureAiSearchVectorClient(IOptions<AzureAiSearchOptions> options)
    {
        _options = options.Value;
        _client = new Lazy<SearchClient>(CreateClient);
    }

    /// <summary>Creates the search client, using the API key when one is set and a managed identity otherwise.</summary>
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

    /// <inheritdoc />
    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        ReadOnlyMemory<float> queryVector,
        string? searchText = null,
        string? sourceBlobName = null,
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

        if (sourceBlobName is not null)
        {
            if (string.IsNullOrWhiteSpace(_options.SourceField))
            {
                throw new CopilotException(
                    "Cannot restrict a search to one document: AzureAiSearch:SourceField " +
                    "is not configured, so there is no field to filter on. Set it to " +
                    "whatever field in the index carries the blob path or name (e.g. " +
                    "metadata_storage_name).");
            }

            searchOptions.Filter =
                $"{_options.SourceField} eq '{EscapeODataStringLiteral(sourceBlobName)}'";
        }

        foreach (var field in select)
        {
            searchOptions.Select.Add(field);
        }

        try
        {
            var response = await _client.Value.SearchAsync<SearchDocument>(
                searchText, searchOptions, cancellationToken);

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

    /// <summary>OData string literals escape an embedded single quote by doubling it.</summary>
    private static string EscapeODataStringLiteral(string value) => value.Replace("'", "''");
}
