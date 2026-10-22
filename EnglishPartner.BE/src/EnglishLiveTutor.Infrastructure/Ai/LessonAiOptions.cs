namespace EnglishLiveTutor.Infrastructure.Ai;

public sealed class LessonGenerationOptions
{
    public const string SectionName = "Ai:LessonGeneration";

    public string ModelCode { get; set; } = "gemini-3.8-flash";
    public int TimeoutSeconds { get; set; } = 120;
    public int MaxRetries { get; set; } = 3;
    public string? ApiKey { get; set; }
}

public sealed class LessonAssessmentOptions
{
    public const string SectionName = "Ai:LessonAssessment";

    public string ModelCode { get; set; } = "gemini-3.8-flash";
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxRetries { get; set; } = 3;
    public string? ApiKey { get; set; }
}

public sealed class LessonRolePlayOptions
{
    public const string SectionName = "Ai:LessonRolePlay";

    public string ModelCode { get; set; } = "gemini-3.8-flash";
    public int TimeoutSeconds { get; set; } = 45;
    public int MaxRetries { get; set; } = 2;
    public string? ApiKey { get; set; }
}
