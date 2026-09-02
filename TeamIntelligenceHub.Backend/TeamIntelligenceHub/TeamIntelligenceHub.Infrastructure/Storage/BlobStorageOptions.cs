using TeamIntelligenceHub.Application.Interfaces;

namespace TeamIntelligenceHub.Infrastructure.Storage;

/// <summary>
/// Binds the "BlobStorage" configuration section.
/// </summary>
/// <remarks>
/// Values resolve through IConfiguration, so appsettings.json, user secrets, and
/// environment variables all work without a code change. Supply either a connection
/// string, or an AccountUri to authenticate with a managed identity instead — the
/// connection string wins when both are present.
/// </remarks>
public class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    /// <summary>Full connection string. Prefer AccountUri plus a managed identity.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>e.g. https://myaccount.blob.core.windows.net</summary>
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

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ConnectionString) ||
        !string.IsNullOrWhiteSpace(AccountUri);

    /// <summary>Maps a storage area onto the container that holds it.</summary>
    public string ResolveContainerName(FileStorageArea area) => area switch
    {
        FileStorageArea.TaskAttachments => ContainerName,
        FileStorageArea.ContributionAttachments => ContributionsContainerName,
        _ => throw new ArgumentOutOfRangeException(
            nameof(area), area, "No container is configured for this storage area.")
    };
}
