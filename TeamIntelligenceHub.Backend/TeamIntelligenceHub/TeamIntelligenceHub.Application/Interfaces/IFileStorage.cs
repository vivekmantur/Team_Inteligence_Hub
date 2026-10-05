namespace TeamIntelligenceHub.Application.Interfaces;

/// <summary>
/// Which body of files an operation concerns.
/// </summary>
/// <remarks>
/// Named in domain terms so the Application layer stays storage-agnostic; Infrastructure
/// maps each area to its own container. Separate containers rather than folders, because
/// access control and search indexing are both scoped per container: contribution
/// evidence grounds Copilot, task comment chatter does not.
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
