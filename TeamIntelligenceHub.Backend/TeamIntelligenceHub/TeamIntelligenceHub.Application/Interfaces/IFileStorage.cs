namespace TeamIntelligenceHub.Application.Interfaces;

/// <summary>
/// Which body of files an operation concerns.
/// </summary>
/// <remarks>
/// Named in domain terms rather than as a container name, so the Application layer stays
/// unaware that Azure Blob Storage is behind it. Infrastructure maps each area to a real
/// container.
///
/// The areas are separate containers rather than folders in one, because the two things
/// that will distinguish them are both container-scoped in Azure: role assignments can be
/// scoped to a container but not to a blob prefix, and an AI Search indexer is configured
/// against a container. Contribution evidence is meant to ground Copilot; task comment
/// chatter is not.
/// </remarks>
public enum FileStorageArea
{
    /// <summary>Files attached to comments on a task.</summary>
    TaskAttachments,

    /// <summary>Files attached to a contribution.</summary>
    ContributionAttachments
}

/// <summary>
/// Somewhere to put the bytes of an uploaded file.
/// </summary>
/// <remarks>
/// Declared here so the Application layer can store and fetch files without knowing that
/// Azure Blob Storage is behind it. Swapping to local disk, S3, or a managed identity
/// instead of a connection string touches only the Infrastructure implementation.
/// </remarks>
public interface IFileStorage
{
    /// <summary>
    /// Stores the stream and returns the key it was stored under.
    /// </summary>
    /// <param name="area">Which body of files this belongs to.</param>
    /// <param name="prefix">
    /// Optional folder path the file is filed under within the area, e.g.
    /// "tasks/3/comments/7". The leaf name is still generated, so an uploaded name can
    /// never traverse or collide.
    /// </param>
    Task<string> UploadAsync(
        FileStorageArea area,
        Stream content,
        string fileName,
        string contentType,
        string? prefix = null,
        CancellationToken cancellationToken = default);

    /// <summary>Opens the stored file for reading.</summary>
    Task<Stream> DownloadAsync(
        FileStorageArea area,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the stored file. Succeeds silently when it is already gone, so cleanup
    /// after a partial failure is safe to retry.
    /// </summary>
    Task DeleteAsync(
        FileStorageArea area,
        string blobName,
        CancellationToken cancellationToken = default);
}
