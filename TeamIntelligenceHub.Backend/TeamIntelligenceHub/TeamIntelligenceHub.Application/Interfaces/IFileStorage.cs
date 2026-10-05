namespace TeamIntelligenceHub.Application.Interfaces;

/// <summary>
/// Which body of files an operation concerns. Infrastructure maps each area to its own
/// container, because access control and search indexing are scoped per container.
/// </summary>
public enum FileStorageArea
{
    /// <summary>Files attached to comments on a task.</summary>
    TaskAttachments,

    /// <summary>Files attached to a contribution.</summary>
    ContributionAttachments
}

/// <summary>
/// Somewhere to put the bytes of an uploaded file, without the Application layer knowing
/// which storage provider is behind it.
/// </summary>
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
    /// <param name="content">The file bytes to store.</param>
    /// <param name="fileName">The original file name, served back on download.</param>
    /// <param name="contentType">The MIME type stored with the file.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The key the file was stored under.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.FileStorageException">
    /// Thrown when the file store rejects the upload.
    /// </exception>
    Task<string> UploadAsync(
        FileStorageArea area,
        Stream content,
        string fileName,
        string contentType,
        string? prefix = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the stored file for reading.
    /// </summary>
    /// <param name="area">Which body of files the file belongs to.</param>
    /// <param name="blobName">The key returned when the file was uploaded.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A readable stream of the file's bytes.</returns>
    /// <exception cref="TeamIntelligenceHub.Application.Exceptions.FileStorageException">
    /// Thrown when the file store cannot read the file.
    /// </exception>
    Task<Stream> DownloadAsync(
        FileStorageArea area,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the stored file. Succeeds silently when it is already gone, so cleanup
    /// after a partial failure is safe to retry.
    /// </summary>
    /// <param name="area">Which body of files the file belongs to.</param>
    /// <param name="blobName">The key returned when the file was uploaded.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task DeleteAsync(
        FileStorageArea area,
        string blobName,
        CancellationToken cancellationToken = default);
}
