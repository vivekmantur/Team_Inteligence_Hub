using System.Text;
using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TestimonialAndCustomerStoryExtractionFunction;

/// <summary>
/// One chunk of indexed content returned by a search, close enough to the query to be
/// worth analyzing.
/// </summary>
public class RetrievedChunk
{
    public string Content { get; set; } = null!;

    public string? Title { get; set; }

    public string? Source { get; set; }

    public double Score { get; set; }
}

/// <summary>
/// Local, self-contained equivalent of the main backend's AzureAiSearchVectorClient — same
/// SDK, same hybrid-search-plus-scoped-filter behavior, no shared code.
/// </summary>
public class DocumentSearchClient
{
    private readonly AzureAiSearchOptions _options;
    private readonly Lazy<SearchClient> _client;
    private readonly ILogger<DocumentSearchClient> _logger;

    public DocumentSearchClient(
        IOptions<AzureAiSearchOptions> options, ILogger<DocumentSearchClient> logger)
    {
        _options = options.Value;
        _client = new Lazy<SearchClient>(CreateClient);
        _logger = logger;
    }

    private SearchClient CreateClient()
    {
        if (!_options.IsConfigured)
        {
            throw new ExtractionException(
                "Azure AI Search is not configured. Set AzureAiSearch:Endpoint and " +
                "AzureAiSearch:IndexName (and AzureAiSearch:ApiKey, unless using a " +
                "managed identity).");
        }

        var endpoint = new Uri(_options.Endpoint!);

        _logger.LogInformation(
            "Creating search client for endpoint {Endpoint}, index {IndexName}, auth={Auth}.",
            endpoint, _options.IndexName,
            string.IsNullOrWhiteSpace(_options.ApiKey) ? "DefaultAzureCredential" : "ApiKey");

        return string.IsNullOrWhiteSpace(_options.ApiKey)
            ? new SearchClient(endpoint, _options.IndexName, new DefaultAzureCredential())
            : new SearchClient(
                endpoint, _options.IndexName, new AzureKeyCredential(_options.ApiKey));
    }

    /// <summary>
    /// Oversampling factor used when a search is scoped to one document: the id fields
    /// Azure AI Search would otherwise let us filter on (text_document_id /
    /// image_document_id) store a hashed value with an undocumented per-page suffix, so
    /// exact server-side filtering isn't reliable. Instead we ask for more results than
    /// we need and narrow to the target document client-side.
    /// </summary>
    private const int ScopedOversampleFactor = 10;
    private const int ScopedOversampleMinimum = 50;

    /// <param name="queryVector">The embedded query to search near.</param>
    /// <param name="searchText">
    /// When supplied, runs a hybrid search — keyword relevance (BM25) fused with vector
    /// relevance — instead of vector search alone.
    /// </param>
    /// <param name="sourceBlobName">
    /// When supplied, restricts the results to chunks belonging to this
    /// ContributionAttachment's blob, instead of the whole index — matched by
    /// reconstructing the id AzureAiSearch:SourceField stores per chunk (see
    /// ComputeBlobDocumentKey) and checking it as a prefix, since the stored value
    /// carries an undocumented page-index suffix after the encoded URL.
    /// </param>
    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        ReadOnlyMemory<float> queryVector,
        string? searchText = null,
        string? sourceBlobName = null,
        CancellationToken cancellationToken = default)
    {
        string? documentKey = null;

        if (sourceBlobName is not null)
        {
            if (string.IsNullOrWhiteSpace(_options.SourceField))
            {
                throw new ExtractionException(
                    "Cannot restrict a search to one document: AzureAiSearch:SourceField is " +
                    "not configured, so there is no field to match against. Set it to the " +
                    "chunk id field the index assigns per source document (e.g. " +
                    "text_document_id).");
            }

            if (string.IsNullOrWhiteSpace(_options.SourceUrlPrefix))
            {
                throw new ExtractionException(
                    "Cannot restrict a search to one document: AzureAiSearch:SourceUrlPrefix " +
                    "is not configured, so a blob name cannot be turned into the encoded " +
                    "value the index actually stores. Set it to the blob container's base " +
                    "URL, e.g. https://<account>.blob.core.windows.net/<container>.");
            }

            documentKey = ComputeBlobDocumentKey(_options.SourceUrlPrefix, sourceBlobName);
        }

        var select = new List<string> { _options.ContentField };

        if (!string.IsNullOrWhiteSpace(_options.TitleField))
        {
            select.Add(_options.TitleField);
        }

        if (!string.IsNullOrWhiteSpace(_options.SourceField))
        {
            select.Add(_options.SourceField);
        }

        var size = sourceBlobName is not null
            ? Math.Max(_options.TopNDocuments * ScopedOversampleFactor, ScopedOversampleMinimum)
            : _options.TopNDocuments;

        var searchOptions = new SearchOptions
        {
            Size = size,
            VectorSearch = new()
            {
                Queries =
                {
                    new VectorizedQuery(queryVector)
                    {
                        KNearestNeighborsCount = size,
                        Fields = { _options.VectorField }
                    }
                }
            }
        };

        foreach (var field in select)
        {
            searchOptions.Select.Add(field);
        }

        _logger.LogInformation(
            "Searching index. searchText=\"{SearchText}\", size={Size}, " +
            "sourceField={SourceField}, sourceBlobName={SourceBlobName}, documentKey={DocumentKey}",
            searchText, size, _options.SourceField, sourceBlobName, documentKey);

        try
        {
            var response = await _client.Value.SearchAsync<SearchDocument>(
                searchText, searchOptions, cancellationToken);

            var chunks = new List<RetrievedChunk>();
            var rawCount = 0;
            var matchedCount = 0;

            await foreach (var result in response.Value.GetResultsAsync())
            {
                rawCount++;

                var source = _options.SourceField is not null
                    && result.Document.TryGetValue(_options.SourceField, out var sourceValue)
                    ? sourceValue?.ToString()
                    : null;

                var isMatch = documentKey is null
                    || (source is not null
                        && source.StartsWith(documentKey, StringComparison.Ordinal));

                _logger.LogInformation(
                    "Raw result #{Index}: score={Score}, source=\"{Source}\", matched={Matched}",
                    rawCount, result.Score, source, isMatch);

                if (!isMatch)
                {
                    continue;
                }

                matchedCount++;

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
                    Source = source,
                    Score = result.Score ?? 0
                });

                if (documentKey is not null && chunks.Count >= _options.TopNDocuments)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "Search complete: {RawCount} raw results, {MatchedCount} matched, " +
                "{ReturnedCount} returned.",
                rawCount, matchedCount, chunks.Count);

            return chunks;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                ex, "Search request failed: {ErrorCode} {Message}",
                ex.ErrorCode ?? ex.Status.ToString(), ex.Message);

            throw new ExtractionException(
                $"Could not search the index: {ex.ErrorCode ?? ex.Status.ToString()}. " +
                $"{ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Reproduces the "Import and vectorize data" wizard's own document-id convention for
    /// a blob-sourced chunk: the full blob URL, UTF-8 encoded, then Base64URL (no
    /// padding). This is a PREFIX of what ends up in fields like text_document_id — the
    /// stored value has an extra, undocumented page-index digit appended with no
    /// delimiter — so callers must match it with StartsWith, never an exact equality
    /// check.
    /// </summary>
    private static string ComputeBlobDocumentKey(string sourceUrlPrefix, string blobName)
    {
        var fullUrl = $"{sourceUrlPrefix.TrimEnd('/')}/{blobName.TrimStart('/')}";
        var bytes = Encoding.UTF8.GetBytes(fullUrl);
        var base64 = Convert.ToBase64String(bytes);

        return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
