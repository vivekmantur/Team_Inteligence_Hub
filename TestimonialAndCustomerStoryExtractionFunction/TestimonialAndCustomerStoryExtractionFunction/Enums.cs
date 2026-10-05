namespace TestimonialAndCustomerStoryExtractionFunction;

/// <summary>
/// Local redeclaration of the main backend's Domain enum of the same name/members. Member
/// names must stay in sync with it — both sides store this as a string in the same
/// DocumentTestimonialsAndCustomerStories.Type column, so the two enums only need to agree
/// on spelling, not share an assembly.
/// </summary>
public enum DocumentInsightType
{
    /// <summary>A customer story with a problem, solution and outcome.</summary>
    CustomerStory,

    /// <summary>A direct quote from a customer or stakeholder.</summary>
    Testimonial
}

/// <summary>
/// Local redeclaration of TeamIntelligenceHub.Domain.Enums.TestimonialAudience. Same
/// column-sharing note as DocumentInsightType applies.
/// </summary>
public enum TestimonialAudience
{
    /// <summary>Company leadership.</summary>
    Leadership,

    /// <summary>A project or business stakeholder.</summary>
    Stakeholder,

    /// <summary>An external customer.</summary>
    Customer,

    /// <summary>A member of an internal team.</summary>
    Team
}

/// <summary>
/// Local redeclaration of TeamIntelligenceHub.Domain.Enums.TestimonialSentiment. Same
/// column-sharing note as DocumentInsightType applies.
/// </summary>
public enum TestimonialSentiment
{
    /// <summary>Favorable feedback.</summary>
    Positive,

    /// <summary>Neither favorable nor critical feedback.</summary>
    Neutral,

    /// <summary>Critical feedback offered to help improve.</summary>
    Constructive
}
