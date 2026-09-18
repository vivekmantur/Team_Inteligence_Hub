using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using FluentAssertions;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Tests.DTOs;

/// <summary>
/// Content Studio's request enums (Format, Tone, Audience, Length) are only meaningful
/// wire values when JSON binding actually rejects a value outside the enum. This is the
/// mechanism ASP.NET Core's [ApiController] automatic 400 response relies on for every
/// other enum-typed request DTO in this codebase (ContributionType, Priority, and so on);
/// these tests confirm ContentGenerationRequestDto gets the same protection for free from
/// the JsonStringEnumConverter registered in Program.cs, with no bespoke validation code.
/// </summary>
public class ContentGenerationRequestDtoTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    [Fact]
    public void Deserialize_WithValidValues_BindsAllFourFields()
    {
        const string json = """
            {
                "Format": "ExecutiveSummary",
                "Tone": "Confident",
                "Audience": "Leadership",
                "Length": "Medium"
            }
            """;

        var request = JsonSerializer.Deserialize<ContentGenerationRequestDto>(json, Options);

        request.Should().NotBeNull();
        request!.Format.Should().Be(ContentFormat.ExecutiveSummary);
        request.Tone.Should().Be(ContentTone.Confident);
        request.Audience.Should().Be(ContentAudience.Leadership);
        request.Length.Should().Be(ContentLength.Medium);
    }

    [Fact]
    public void Deserialize_WithInvalidFormat_Throws()
    {
        const string json = """{"Format": "NotAFormat", "Tone": "Confident", "Audience": "Leadership", "Length": "Medium"}""";

        var act = () => JsonSerializer.Deserialize<ContentGenerationRequestDto>(json, Options);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Deserialize_WithInvalidTone_Throws()
    {
        const string json = """{"Format": "Blog", "Tone": "NotATone", "Audience": "Leadership", "Length": "Medium"}""";

        var act = () => JsonSerializer.Deserialize<ContentGenerationRequestDto>(json, Options);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Deserialize_WithInvalidAudience_Throws()
    {
        const string json = """{"Format": "Blog", "Tone": "Confident", "Audience": "NotAnAudience", "Length": "Medium"}""";

        var act = () => JsonSerializer.Deserialize<ContentGenerationRequestDto>(json, Options);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Deserialize_WithInvalidLength_Throws()
    {
        const string json = """{"Format": "Blog", "Tone": "Confident", "Audience": "Leadership", "Length": "NotALength"}""";

        var act = () => JsonSerializer.Deserialize<ContentGenerationRequestDto>(json, Options);

        act.Should().Throw<JsonException>();
    }

    [Theory]
    [InlineData(ContentFormat.LinkedInPost)]
    [InlineData(ContentFormat.VivaEngagePost)]
    [InlineData(ContentFormat.Newsletter)]
    [InlineData(ContentFormat.ExecutiveSummary)]
    [InlineData(ContentFormat.QbrSlide)]
    [InlineData(ContentFormat.Blog)]
    [InlineData(ContentFormat.CaseStudy)]
    public void ContentFormat_HasAllSevenMembers(ContentFormat format)
    {
        // Enumerates every required member so a future accidental rename or removal
        // fails this test instead of silently dropping a format.
        Enum.IsDefined(format).Should().BeTrue();
    }

    private static ContentGenerationRequestDto ValidRequest(List<ContentGenerationTurnDto>? previousTurns = null) =>
        new()
        {
            Format = ContentFormat.Blog,
            Tone = ContentTone.Confident,
            Audience = ContentAudience.Leadership,
            Length = ContentLength.Medium,
            Instructions = "Emphasize measurable impact.",
            PreviousTurns = previousTurns,
        };

    private static List<ValidationResult> Validate(ContentGenerationRequestDto request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        // Validator.TryValidateObject, called directly, does not recurse into nested
        // complex types or collections — only ASP.NET Core's [ApiController] automatic
        // model validation does that (via its ValidationVisitor). Replicate that
        // recursion here so this test exercises ContentGenerationTurnDto's own
        // [StringLength] attributes the same way a real HTTP request would.
        if (request.PreviousTurns is not null)
        {
            foreach (var turn in request.PreviousTurns)
            {
                Validator.TryValidateObject(
                    turn,
                    new ValidationContext(turn),
                    results,
                    validateAllProperties: true);
            }
        }

        return results;
    }

    private static ContentGenerationTurnDto Turn(string instruction = "Make it shorter.", string output = "Previous output.") =>
        new() { Instruction = instruction, Output = output };

    [Fact]
    public void Validate_WithMissingRequiredFields_StillFailsExistingValidation()
    {
        var request = new ContentGenerationRequestDto();

        var results = Validate(request);

        results.Should().Contain(r => r.MemberNames.Contains(nameof(ContentGenerationRequestDto.Format)));
        results.Should().Contain(r => r.MemberNames.Contains(nameof(ContentGenerationRequestDto.Tone)));
        results.Should().Contain(r => r.MemberNames.Contains(nameof(ContentGenerationRequestDto.Audience)));
        results.Should().Contain(r => r.MemberNames.Contains(nameof(ContentGenerationRequestDto.Length)));
    }

    [Fact]
    public void Validate_WithInstructionsOverLimit_Fails()
    {
        var request = ValidRequest();
        request.Instructions = new string('a', ContentGenerationRequestDto.InstructionsMaxLength + 1);

        var results = Validate(request);

        results.Should().Contain(r => r.MemberNames.Contains(nameof(ContentGenerationRequestDto.Instructions)));
    }

    [Fact]
    public void Validate_WithNullPreviousTurns_HasNoPreviousTurnsErrors()
    {
        var request = ValidRequest(previousTurns: null);

        var results = Validate(request);

        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyPreviousTurns_HasNoPreviousTurnsErrors()
    {
        var request = ValidRequest(previousTurns: new List<ContentGenerationTurnDto>());

        var results = Validate(request);

        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithPreviousTurnsWithinLimits_Passes()
    {
        var request = ValidRequest(previousTurns: new List<ContentGenerationTurnDto>
        {
            Turn("First instruction.", "First output."),
            Turn("Second instruction.", "Second output."),
        });

        var results = Validate(request);

        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithExactlyMaxPreviousTurns_Passes()
    {
        var turns = Enumerable.Range(1, ContentGenerationRequestDto.MaxPreviousTurns)
            .Select(i => Turn($"Instruction {i}.", $"Output {i}."))
            .ToList();
        var request = ValidRequest(previousTurns: turns);

        var results = Validate(request);

        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithMoreThanMaxPreviousTurns_Fails()
    {
        var turns = Enumerable.Range(1, ContentGenerationRequestDto.MaxPreviousTurns + 1)
            .Select(i => Turn($"Instruction {i}.", $"Output {i}."))
            .ToList();
        var request = ValidRequest(previousTurns: turns);

        var results = Validate(request);

        results.Should().Contain(r => r.MemberNames.Contains(nameof(ContentGenerationRequestDto.PreviousTurns)));
    }

    [Fact]
    public void Validate_WithPreviousTurnInstructionOverLimit_Fails()
    {
        var request = ValidRequest(previousTurns: new List<ContentGenerationTurnDto>
        {
            Turn(new string('a', ContentGenerationTurnDto.InstructionMaxLength + 1), "Output."),
        });

        var results = Validate(request);

        results.Should().Contain(r => r.MemberNames.Contains(nameof(ContentGenerationTurnDto.Instruction)));
    }

    [Fact]
    public void Validate_WithPreviousTurnOutputOverLimit_Fails()
    {
        var request = ValidRequest(previousTurns: new List<ContentGenerationTurnDto>
        {
            Turn("Instruction.", new string('a', ContentGenerationTurnDto.OutputMaxLength + 1)),
        });

        var results = Validate(request);

        results.Should().Contain(r => r.MemberNames.Contains(nameof(ContentGenerationTurnDto.Output)));
    }

    [Fact]
    public void Validate_WithTotalPreviousTurnsLengthOverLimit_Fails()
    {
        // Each turn stays within the per-field 1000-char cap and the request stays within
        // the 8-turn cap, but the combined total across all turns exceeds the aggregate
        // 8000-char limit (8 turns x 1100 chars = 8800).
        var turns = Enumerable.Range(1, 8)
            .Select(_ => Turn(new string('a', 600), new string('b', 500)))
            .ToList();
        var request = ValidRequest(previousTurns: turns);

        var results = Validate(request);

        results.Should().Contain(r => r.MemberNames.Contains(nameof(ContentGenerationRequestDto.PreviousTurns)));
    }

    [Fact]
    public void Validate_WithTotalPreviousTurnsLengthExactlyAtLimit_Passes()
    {
        var turns = new List<ContentGenerationTurnDto>
        {
            Turn(new string('a', 500), new string('b', 500)),
            Turn(new string('a', 500), new string('b', 500)),
            Turn(new string('a', 500), new string('b', 500)),
            Turn(new string('a', 500), new string('b', 500)),
            Turn(new string('a', 500), new string('b', 500)),
            Turn(new string('a', 500), new string('b', 500)),
            Turn(new string('a', 500), new string('b', 500)),
            Turn(new string('a', 500), new string('b', 500)),
        };
        var request = ValidRequest(previousTurns: turns);

        var results = Validate(request);

        results.Should().BeEmpty();
    }
}
