// 1. Parse a customer story reply
// 2. Parse a testimonial reply

namespace TestimonialAndCustomerStoryExtractionFunction;

/// <summary>
/// The customer story fields parsed from the model's reply.
/// </summary>
/// <param name="CustomerName">The external customer or account name.</param>
/// <param name="Summary">A short summary of the story.</param>
/// <param name="Outcome">The headline result.</param>
/// <param name="Quote">A direct quote from the customer.</param>
/// <param name="BusinessValue">The business value or impact.</param>
public sealed record ExtractedCustomerStory(
    string? CustomerName,
    string? Summary,
    string? Outcome,
    string? Quote,
    string? BusinessValue);

/// <summary>
/// The testimonial fields parsed from the model's reply.
/// </summary>
/// <param name="Quote">The quote, in the speaker's own words.</param>
/// <param name="SpeakerName">The speaker's name.</param>
/// <param name="SpeakerRole">The speaker's role or title.</param>
/// <param name="Audience">The audience group the speaker belongs to.</param>
/// <param name="Sentiment">The tone of the feedback.</param>
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
    /// <summary>
    /// Parses a customer story reply into its fields.
    /// </summary>
    /// <param name="modelResponse">The model's raw reply to the customer story prompt.</param>
    /// <returns>
    /// The parsed customer story, or null when the model found none or every field is empty.
    /// </returns>
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

    /// <summary>
    /// Parses a testimonial reply into its fields.
    /// </summary>
    /// <param name="modelResponse">The model's raw reply to the testimonial prompt.</param>
    /// <returns>
    /// The parsed testimonial, or null when the model found none or the reply has no quote.
    /// </returns>
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

    /// <summary>Splits the reply into LABEL: value pairs, keyed case-insensitively.</summary>
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

    /// <summary>Returns the trimmed value for a label, or null when it is missing, blank or NONE.</summary>
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
