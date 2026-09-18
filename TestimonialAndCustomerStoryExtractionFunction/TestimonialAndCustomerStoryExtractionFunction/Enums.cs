namespace TestimonialAndCustomerStoryExtractionFunction;

/// <summary>
/// Local redeclaration of the main backend's Domain enum of the same name/members. Member
/// names must stay in sync with it — both sides store this as a string in the same
/// DocumentTestimonialsAndCustomerStories.Type column, so the two enums only need to agree
/// on spelling, not share an assembly.
/// </summary>
public enum DocumentInsightType
{
    CustomerStory,
    Testimonial
}

/// <summary>
/// Local redeclaration of TeamIntelligenceHub.Domain.Enums.TestimonialAudience. Same
/// column-sharing note as DocumentInsightType applies.
/// </summary>
public enum TestimonialAudience
{
    Leadership,
    Stakeholder,
    Customer,
    Team
}

/// <summary>
/// Local redeclaration of TeamIntelligenceHub.Domain.Enums.TestimonialSentiment. Same
/// column-sharing note as DocumentInsightType applies.
/// </summary>
public enum TestimonialSentiment
{
    Positive,
    Neutral,
    Constructive
}
