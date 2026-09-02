using System.ComponentModel.DataAnnotations;
using TeamIntelligenceHub.Domain.Entities;
using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Application.DTOs;

/// <summary>
/// Payload for creating an Initiative.
/// </summary>
/// <remarks>
/// Limits come from the <see cref="Initiative"/> constants, so a value can never pass
/// validation here only to fail on truncation at SQL. These annotations cover shape —
/// presence, length, range. Rules that need the database (does this owner exist, is the
/// name taken) live in InitiativeService.
///
/// Dates are nullable on purpose. [Required] cannot detect an omitted non-nullable
/// DateOnly, because model binding fills it with 0001-01-01 rather than leaving it null.
/// </remarks>
public class CreateInitiativeRequestDto
{
    [Required(ErrorMessage = "Initiative Name is required.")]
    [StringLength(
        Initiative.NameMaxLength,
        MinimumLength = Initiative.NameMinLength,
        ErrorMessage = "Initiative Name must be between {2} and {1} characters.")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(
        Initiative.DescriptionMaxLength,
        MinimumLength = Initiative.DescriptionMinLength,
        ErrorMessage = "Description must be between {2} and {1} characters.")]
    public string Description { get; set; } = null!;

    /// <summary>Which area of work this Initiative primarily supports. An unknown value is a 400.</summary>
    [Required(ErrorMessage = "Business Area is required.")]
    public InitiativeFocusArea? BusinessArea { get; set; }

    /// <summary>What form the work takes. An unknown value is a 400.</summary>
    [Required(ErrorMessage = "Initiative Type is required.")]
    public InitiativeWorkform? InitiativeType { get; set; }

    /// <summary>Defaults to Medium when omitted. An unknown value is a 400.</summary>
    public InitiativePriority? Priority { get; set; }

    /// <summary>Must reference an existing user. Defaults to the caller when omitted.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Initiative Owner is not a valid user.")]
    public int? OwnerUserId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Executive Sponsor is not a valid user.")]
    public int? ExecutiveSponsorUserId { get; set; }

    /// <summary>Defaults to Enterprise when omitted.</summary>
    public InitiativeSegment? Segment { get; set; }

    /// <summary>Enterprise roles this Initiative's change lands on. Defaults to empty when omitted.</summary>
    public List<EnterpriseRole>? ImpactedRoles { get; set; }

    /// <summary>Defaults to Medium when omitted.</summary>
    public InitiativeChangeImpact? ChangeImpact { get; set; }

    [Required(ErrorMessage = "Start Date is required.")]
    public DateOnly? StartDate { get; set; }

    [Required(ErrorMessage = "Target End Date is required.")]
    public DateOnly? TargetEndDate { get; set; }

    /// <summary>Defaults to Assess when omitted.</summary>
    public InitiativeLifecycleStage? LifecycleStage { get; set; }

    /// <summary>Defaults to OnTrack when omitted.</summary>
    public InitiativeHealth? Health { get; set; }

    /// <summary>Defaults to Active when omitted.</summary>
    public InitiativeStatus? Status { get; set; }

    [StringLength(Initiative.LongTextMaxLength)]
    public string? KeyObjective { get; set; }

    [StringLength(Initiative.LongTextMaxLength)]
    public string? ExpectedOutcome { get; set; }

    [StringLength(Initiative.LongTextMaxLength)]
    public string? SuccessMeasures { get; set; }
}
