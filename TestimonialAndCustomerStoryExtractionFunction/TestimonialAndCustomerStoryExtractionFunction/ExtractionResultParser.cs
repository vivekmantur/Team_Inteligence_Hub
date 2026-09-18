namespace TestimonialAndCustomerStoryExtractionFunction;

public sealed record ExtractedCustomerStory(
    string? CustomerName,
    string? Summary,
    string? Outcome,
    string? Quote,
    string? BusinessValue);

public sealed record ExtractedTestimonial(
    string Quote,
    string? SpeakerName,
    string? SpeakerRole,
    TestimonialAudience? Audience,
    TestimonialSentiment? Sentiment);

/// <summary>
/// Parses the labeled-line format <see cref="ExtractionPromptBuilder"/> asks the model
/// for. IChatCompletionClient returns plain text only (no structured/JSON output mode),
/// so this is a small, forgiving line parser rather than a JSON deserializer.
/// </summary>
public static class ExtractionResultParser
{
    public static ExtractedCustomerStory? ParseCustomerStory(string modelResponse)
    {
        if (IsNoInsightFound(modelResponse))
        {
            return null;
        }

        var fields = ParseLabeledLines(modelResponse);

        var extracted = new ExtractedCustomerStory(
            CustomerName: GetOrNull(fields, "CUSTOMER"),
            Summary: GetOrNull(fields, "SUMMARY"),
            Outcome: GetOrNull(fields, "OUTCOME"),
            Quote: GetOrNull(fields, "QUOTE"),
            BusinessValue: GetOrNull(fields, "BUSINESSVALUE"));

        // If the model's response matched neither the NO_INSIGHT_FOUND marker nor the
        // labeled-line format (e.g. it added commentary despite being told not to), every
        // field above comes back null. Treat that the same as "nothing found" rather than
        // saving an empty row — the Testimonial side gets this for free via its required
        // Quote field; CustomerStory has no single required field, so it needs its own
        // check.
        var isEmpty = extracted.CustomerName is null
            && extracted.Summary is null
            && extracted.Outcome is null
            && extracted.Quote is null
            && extracted.BusinessValue is null;

        return isEmpty ? null : extracted;
    }

    public static ExtractedTestimonial? ParseTestimonial(string modelResponse)
    {
        if (IsNoInsightFound(modelResponse))
        {
            return null;
        }

        var fields = ParseLabeledLines(modelResponse);
        var quote = GetOrNull(fields, "QUOTE");

        // A "testimonial" with no actual quote text is not a testimonial.
        if (quote is null)
        {
            return null;
        }

        return new ExtractedTestimonial(
            Quote: quote,
            SpeakerName: GetOrNull(fields, "NAME"),
            SpeakerRole: GetOrNull(fields, "ROLE"),
            Audience: ParseEnumOrNull<TestimonialAudience>(GetOrNull(fields, "AUDIENCE")),
            Sentiment: ParseEnumOrNull<TestimonialSentiment>(GetOrNull(fields, "SENTIMENT")));
    }

    private static bool IsNoInsightFound(string response) =>
        response.Trim().Equals(
            ExtractionPromptBuilder.NoInsightFoundMarker,
            StringComparison.OrdinalIgnoreCase);

    private static Dictionary<string, string> ParseLabeledLines(string response)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in response.Split('\n'))
        {
            var separatorIndex = line.IndexOf(':');

            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            if (key.Length > 0)
            {
                result[key] = value;
            }
        }

        return result;
    }

    private static string? GetOrNull(Dictionary<string, string> fields, string key)
    {
        if (!fields.TryGetValue(key, out var value))
        {
            return null;
        }

        var trimmed = value.Trim();

        return trimmed.Length == 0 || trimmed.Equals("NONE", StringComparison.OrdinalIgnoreCase)
            ? null
            : trimmed;
    }

    private static TEnum? ParseEnumOrNull<TEnum>(string? value) where TEnum : struct, Enum =>
        value is not null && Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : null;
}
