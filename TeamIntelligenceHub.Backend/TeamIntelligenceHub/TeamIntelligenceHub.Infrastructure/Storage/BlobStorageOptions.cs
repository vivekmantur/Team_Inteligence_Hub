using TeamIntelligenceHub.Application.Interfaces;

namespace TeamIntelligenceHub.Infrastructure.Storage;

/// <summary>
/// Binds the "BlobStorage" configuration section. Supply a connection string, or an
/// AccountUri for managed identity; the connection string wins when both are present.
/// </summary>
public class BlobStorageOptions
{
    /// <summary>The name of the configuration section these options bind to.</summary>
    public const string SectionName = "BlobStorage";

    /// <summary>The full storage account connection string. Prefer AccountUri plus a managed identity.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>The blob service URI used with a managed identity, e.g. https://myaccount.blob.core.windows.net.</summary>
    public string? AccountUri { get; set; }

    /// <summary>
    /// Container for files attached to task comments.
    /// </summary>
    /// <remarks>
    /// Kept under its original key rather than renamed to match the newer setting, so an
    /// existing deployment does not lose its binding and strand the blobs already in it.
    /// </remarks>
    public string ContainerName { get; set; } = "task-comment-attachments";

    /// <summary>
    /// Container for files attached to contributions.
    /// </summary>
    /// <remarks>
    /// Separate from task attachments because role assignments and AI Search indexers are
    /// both scoped to a container in Azure, and contribution evidence is the corpus meant
    /// to ground Copilot while task comment chatter is not.
    /// </remarks>
    public string ContributionsContainerName { get; set; } = "contribution-attachments";

    /// <summary>Gets a value indicating whether a connection string or an account URI is set.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ConnectionString) ||
        !string.IsNullOrWhiteSpace(AccountUri);

    /// <summary>
    /// Maps a storage area onto the container that holds it.
    /// </summary>
    /// <param name="area">The storage area to resolve.</param>
    /// <returns>The name of the container for that area.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when no container is configured for the area.</exception>
    public string ResolveContainerName(FileStorageArea area) => area switch
    {
        FileStorageArea.TaskAttachments => ContainerName,
        FileStorageArea.ContributionAttachments => ContributionsContainerName,
        _ => throw new ArgumentOutOfRangeException(
            nameof(area), area, "No container is configured for this storage area.")
    };
}
