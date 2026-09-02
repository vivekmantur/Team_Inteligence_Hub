namespace TeamIntelligenceHub.Domain.Entities;

/// <summary>
/// Metadata for a file attached to a contribution.
/// </summary>
/// <remarks>
/// Mirrors TaskCommentAttachment column for column, so the existing IFileStorage
/// abstraction and download endpoint carry over unchanged. The file itself lives in Azure
/// Blob Storage; BlobName is the key within the container, kept separate from FileName so
/// the stored object can be named safely and uniquely without losing what the person
/// called it.
/// </remarks>
public class ContributionAttachment
{
    public const int FileNameMaxLength = 255;
    public const int BlobNameMaxLength = 500;
    public const int ContentTypeMaxLength = 100;

    /// <summary>Upload ceiling, 25 MB. Enforced before anything reaches storage.</summary>
    public const long MaxFileSizeBytes = 25L * 1024 * 1024;

    public int Id { get; set; }

    public int ContributionId { get; set; }

    /// <summary>The name the person uploaded, shown in the UI.</summary>
    public string FileName { get; set; } = null!;

    /// <summary>The key within the blob container.</summary>
    public string BlobName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    /// <summary>Bytes. Formatted for display by the client, never stored formatted.</summary>
    public long FileSize { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties

    public Contribution Contribution { get; set; } = null!;
}
