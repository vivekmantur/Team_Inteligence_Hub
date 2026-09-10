using System.ComponentModel.DataAnnotations;
using System.Linq;
using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Application.DTOs;

// ---------------------------------------------------------------------------
// Requests
// ---------------------------------------------------------------------------

/// <summary>
/// Payload for generating one piece of Content Studio output against an Initiative.
/// </summary>
/// <remarks>
/// The Initiative comes from the route, never the body, matching
/// CreateContributionRequestDto's split with ContributionsController. Carries only the
/// four generation selections — no Contribution, Metric, Risk, Customer Story, AI
/// Practice, or Attachment record or id is ever accepted here; the backend loads all of
/// that itself once the Initiative is confirmed to exist.
/// </remarks>
public class ContentGenerationRequestDto : IValidatableObject
{
    public const int InstructionsMaxLength = 1000;

    [Required(ErrorMessage = "Format is required.")]
    public ContentFormat? Format { get; set; }

    [Required(ErrorMessage = "Tone is required.")]
    public ContentTone? Tone { get; set; }

    [Required(ErrorMessage = "Audience is required.")]
    public ContentAudience? Audience { get; set; }

    [Required(ErrorMessage = "Length is required.")]
    public ContentLength? Length { get; set; }

    /// <summary>
    /// Optional free-text guidance from the caller — what to emphasize, a style note, a
    /// constraint to honor. Unlike the Contribution/Initiative data ContentPromptBuilder
    /// renders, this is deliberately meant to steer the model, not just describe facts —
    /// but it is still free text from an HTTP request, so it is capped in length here and
    /// rendered as labeled content in the user prompt, never folded into the fixed system
    /// prompt.
    /// </summary>
    [StringLength(
        InstructionsMaxLength,
        ErrorMessage = "Instructions cannot exceed {1} characters.")]
    public string? Instructions { get; set; }

    public const int MaxPreviousTurns = 8;
    public const int MaxTotalPreviousTurnsLength = 8000;

    /// <summary>
    /// Prior successful instruction/output pairs from the same Content Studio session
    /// (same Initiative and format), oldest first. Null or empty means no history — the
    /// first generation in a session. Treated as untrusted user-provided context by the
    /// prompt builder, never as system instructions.
    /// </summary>
    public List<ContentGenerationTurnDto>? PreviousTurns { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PreviousTurns is null || PreviousTurns.Count == 0)
        {
            yield break;
        }

        if (PreviousTurns.Count > MaxPreviousTurns)
        {
            yield return new ValidationResult(
                $"PreviousTurns cannot contain more than {MaxPreviousTurns} turns.",
                new[] { nameof(PreviousTurns) });
        }

        var totalLength = PreviousTurns.Sum(turn =>
            (turn.Instruction?.Length ?? 0) + (turn.Output?.Length ?? 0));

        if (totalLength > MaxTotalPreviousTurnsLength)
        {
            yield return new ValidationResult(
                $"PreviousTurns cannot exceed {MaxTotalPreviousTurnsLength} total characters across all turns.",
                new[] { nameof(PreviousTurns) });
        }
    }
}

/// <summary>
/// One prior successful instruction/output pair carried in
/// <see cref="ContentGenerationRequestDto.PreviousTurns"/>.
/// </summary>
public sealed class ContentGenerationTurnDto
{
    public const int InstructionMaxLength = 1000;
    public const int OutputMaxLength = 1000;

    [StringLength(
        InstructionMaxLength,
        ErrorMessage = "Previous turn instruction cannot exceed {1} characters.")]
    public string Instruction { get; set; } = string.Empty;

    [StringLength(
        OutputMaxLength,
        ErrorMessage = "Previous turn output cannot exceed {1} characters.")]
    public string Output { get; set; } = string.Empty;
}

// ---------------------------------------------------------------------------
// Responses
// ---------------------------------------------------------------------------

public class ContentGenerationResponseDto
{
    public string Content { get; set; } = null!;

    public ContentFormat Format { get; set; }

    public int InitiativeId { get; set; }

    /// <summary>
    /// Always false in this implementation. No format retrieves attached documents yet —
    /// Blog and Case Study generate from structured fields only until Initiative-scoped
    /// RAG ships as its own follow-up feature.
    /// </summary>
    public bool UsedDocumentRetrieval { get; set; }

    /// <summary>Always 0 in this implementation, for the same reason as UsedDocumentRetrieval.</summary>
    public int SourceCount { get; set; }

    /// <summary>
    /// Null until content persistence is separately approved — there is no stored row for
    /// this generation to be an id of yet.
    /// </summary>
    public string? GenerationId { get; set; }
}
