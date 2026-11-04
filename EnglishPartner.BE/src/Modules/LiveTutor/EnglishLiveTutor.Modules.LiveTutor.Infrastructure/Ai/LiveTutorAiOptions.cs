namespace EnglishLiveTutor.Infrastructure.Ai;

/// <summary>
/// Timeouts and model codes for the AI helpers that run inside a live tutor session (text translation fallback and grammar
/// correction hints). They stay short on purpose: a hung provider call would otherwise stall the session.
/// </summary>
public sealed class LiveTutorAiOptions
{
    public const string SectionName = "Ai:LiveTutor";
    public const int DefaultTranslationFallbackTimeoutSeconds = 8;
    public const int DefaultGrammarCorrectionTimeoutSeconds = 20;
    public const string DefaultCorrectionModelCode = "gemini-3.8-flash";

    public int TranslationFallbackTimeoutSeconds { get; set; } = DefaultTranslationFallbackTimeoutSeconds;
    public int GrammarCorrectionTimeoutSeconds { get; set; } = DefaultGrammarCorrectionTimeoutSeconds;
    public string CorrectionModelCode { get; set; } = DefaultCorrectionModelCode;
}
