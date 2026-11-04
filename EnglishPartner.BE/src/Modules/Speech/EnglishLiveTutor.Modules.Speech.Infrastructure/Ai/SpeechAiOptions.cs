namespace EnglishLiveTutor.Infrastructure.Ai;

/// <summary>HTTP timeout for Gemini pronunciation assessment (POST /api/pronunciation/assessments).</summary>
public sealed class PronunciationAssessmentOptions
{
    public const string SectionName = "Ai:PronunciationAssessment";

    public int TimeoutSeconds { get; set; } = 600;
}

/// <summary>How long pronunciation TTS waits for the Gemini Live turn to complete before returning what it has.</summary>
public sealed class PronunciationTtsOptions
{
    public const string SectionName = "Ai:PronunciationTts";
    public const int DefaultTimeoutSeconds = 600;

    public int TimeoutSeconds { get; set; } = DefaultTimeoutSeconds;
}
