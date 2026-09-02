using System.Collections.Concurrent;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;

namespace TeamIntelligenceHub.Infrastructure.Storage;

/// <summary>
/// Stores attachments in Azure Blob Storage, one container per storage area.
/// </summary>
public class AzureBlobFileStorage : IFileStorage
{
    private readonly BlobStorageOptions _options;
    private readonly ILogger<AzureBlobFileStorage> _logger;

    /// <summary>
    /// One lazily-created client per area, so a container is only reached when something
    /// actually uses it.
    /// </summary>
    private readonly ConcurrentDictionary<FileStorageArea, Lazy<BlobContainerClient>>
        _containers = new();

    public AzureBlobFileStorage(
        IOptions<BlobStorageOptions> options,
        ILogger<AzureBlobFileStorage> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Resolves the client for an area, creating the container on first use.
    /// </summary>
    /// <remarks>
    /// Lazy so the API still starts when storage is unconfigured — only attachment
    /// endpoints fail, and they say what is missing.
    ///
    /// PublicationOnly matters: the default mode caches the exception, so one bad
    /// connection string would keep failing for the life of the process even after the
    /// configuration was corrected. GetOrAdd may build a spare Lazy under contention,
    /// which costs nothing, because the Lazy is what guards the expensive part.
    /// </remarks>
    private BlobContainerClient Container(FileStorageArea area)
    {
        return _containers.GetOrAdd(
            area,
            key => new Lazy<BlobContainerClient>(
                () => CreateContainerClient(_options.ResolveContainerName(key)),
                LazyThreadSafetyMode.PublicationOnly)).Value;
    }

    private BlobContainerClient CreateContainerClient(string containerName)
    {
        if (!_options.IsConfigured)
        {
            throw new FileStorageException(
                "Blob storage is not configured. Set BlobStorage:ConnectionString " +
                "(or BlobStorage:AccountUri for managed identity).");
        }

        try
        {
            var serviceClient = !string.IsNullOrWhiteSpace(_options.ConnectionString)
                ? new BlobServiceClient(_options.ConnectionString)
                : new BlobServiceClient(
                    new Uri(_options.AccountUri!),
                    new DefaultAzureCredential());

            var container = serviceClient.GetBlobContainerClient(containerName);

            // Private by default — attachments are read back through the API, which
            // checks the caller first. A public container would make files guessable.
            container.CreateIfNotExists(PublicAccessType.None);

            return container;
        }
        catch (Exception ex) when (ex is not FileStorageException)
        {
            _logger.LogError(
                ex, "Could not reach blob container {Container}", containerName);

            throw new FileStorageException(
                $"Could not reach blob container '{containerName}'. " +
                $"Check BlobStorage settings and that the account is reachable. " +
                $"({ex.GetType().Name}: {ex.Message})",
                ex);
        }
    }

    public async Task<string> UploadAsync(
        FileStorageArea area,
        Stream content,
        string fileName,
        string contentType,
        string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        // The leaf name is GUID-anchored, never taken from the upload verbatim. An
        // attacker-supplied name could otherwise traverse the container or overwrite
        // someone else's file. A sanitized slug of the original name is appended after
        // the GUID purely so the blob's own name stays readable downstream (e.g. in
        // Azure AI Search citations, which index metadata_storage_name) — it plays no
        // role in uniqueness or path safety, both of which the GUID alone guarantees.
        //
        // Separators are written literally rather than through a date format string,
        // where "/" means "the current culture's date separator" and would produce a
        // different key on a machine with different regional settings.
        var extension = Path.GetExtension(fileName);
        var slug = SanitizeForBlobName(Path.GetFileNameWithoutExtension(fileName));
        var leaf = string.IsNullOrEmpty(slug)
            ? $"{Guid.NewGuid():N}{extension}"
            : $"{Guid.NewGuid():N}_{slug}{extension}";

        var blobName = string.IsNullOrWhiteSpace(prefix)
            ? leaf
            : $"{prefix.Trim('/')}/{leaf}";

        try
        {
            var blob = Container(area).GetBlobClient(blobName);

            await blob.UploadAsync(
                content,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType,
                        // Serve the original name on download without letting it decide
                        // where the object lives.
                        ContentDisposition =
                            $"attachment; filename=\"{Path.GetFileName(fileName)}\""
                    }
                },
                cancellationToken);

            _logger.LogInformation(
                "Uploaded {FileName} as {BlobName} in {Area}", fileName, blobName, area);

            return blobName;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Upload of {FileName} failed", fileName);

            throw new FileStorageException(
                $"Upload failed: {ex.ErrorCode ?? ex.Status.ToString()}. {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Keeps only characters safe in a blob name segment, so the result can never
    /// introduce a path separator or other traversal-relevant character regardless of
    /// what the original filename contained. Truncated so a very long original name
    /// cannot push the blob key toward Azure's length limit.
    /// </summary>
    private static string SanitizeForBlobName(string name)
    {
        var sanitized = new string(
            name.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());

        return sanitized.Length > 80 ? sanitized[..80] : sanitized;
    }

    public async Task<Stream> DownloadAsync(
        FileStorageArea area,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var blob = Container(area).GetBlobClient(blobName);

            var response = await blob.DownloadStreamingAsync(
                cancellationToken: cancellationToken);

            return response.Value.Content;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Download of {BlobName} failed", blobName);

            throw new FileStorageException(
                $"Could not read the stored file. {ex.Message}", ex);
        }
    }

    public async Task DeleteAsync(
        FileStorageArea area,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await Container(area).GetBlobClient(blobName)
                .DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            // Cleanup is best effort. A file that cannot be removed must not block the
            // database row from going, or the row and the blob drift apart permanently.
            _logger.LogWarning(ex, "Could not delete blob {BlobName}", blobName);
        }
    }
}
