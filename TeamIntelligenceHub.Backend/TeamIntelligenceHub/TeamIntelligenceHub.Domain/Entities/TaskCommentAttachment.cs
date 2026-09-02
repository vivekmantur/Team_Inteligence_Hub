namespace TeamIntelligenceHub.Domain.Entities;

/// <summary>
/// Metadata for a file attached to a comment.
/// </summary>
/// <remarks>
/// The file itself lives in Azure Blob Storage; this row records where. BlobName is the
/// key within the container, kept separate from FileName so the stored object can be
/// named safely and uniquely without losing what the person called it.
/// </remarks>
public class TaskCommentAttachment
{
    public const int FileNameMaxLength = 255;
    public const int BlobNameMaxLength = 500;
    public const int ContentTypeMaxLength = 100;

    /// <summary>Upload ceiling, 25 MB. Enforced before anything reaches storage.</summary>
    public const long MaxFileSizeBytes = 25L * 1024 * 1024;

    public int Id { get; set; }

    public int TaskCommentId { get; set; }

    /// <summary>The name the person uploaded, shown in the UI.</summary>
    public string FileName { get; set; } = null!;

    /// <summary>The key within the blob container.</summary>
    public string BlobName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long FileSize { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties

    public TaskComment TaskComment { get; set; } = null!;
}
