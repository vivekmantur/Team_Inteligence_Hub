namespace TeamIntelligenceHub.Domain.Entities;

/// <summary>
/// Metadata for a file attached to a contribution. It mirrors TaskCommentAttachment, so
/// both attachment kinds share the same IFileStorage abstraction.
/// </summary>
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
