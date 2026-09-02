namespace TeamIntelligenceHub.Application.DTOs;

public class CopilotQuestionRequestDto
{
    public string Question { get; set; } = null!;
}

public class CopilotAnswerDto
{
    public string Answer { get; set; } = null!;

    public List<CopilotCitationDto> Citations { get; set; } = new();

    /// <summary>
    /// Populated only when nothing relevant was found for the question — clearer
    /// phrasings the user can pick from and resend instead of typing a new question.
    /// </summary>
    public List<string> SuggestedQuestions { get; set; } = new();
}

public class CopilotCitationDto
{
    public string? Title { get; set; }

    public string? Source { get; set; }

    public string Content { get; set; } = null!;
}
