using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Domain.Entities;

/// <summary>
/// A pointer to evidence that lives somewhere else. Both of the wizard's link inputs land
/// here, so every URL has one place to look.
/// </summary>
public class ContributionLink
{
    public const int UrlMaxLength = 2048;
    public const int LabelMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int EnumValueMaxLength = 50;

    public int Id { get; set; }

    public int ContributionId { get; set; }

    public ContributionLinkSource Source { get; set; }

    /// <summary>Validated as http or https before it is stored.</summary>
    public string Url { get; set; } = null!;

    /// <summary>
    /// What to show instead of the raw URL. The wizard falls back to the URL when this is
    /// blank, which is a display decision and stays in the client.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>Absorbed from the Supporting Asset section of step 4.</summary>
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties

    public Contribution Contribution { get; set; } = null!;
}
