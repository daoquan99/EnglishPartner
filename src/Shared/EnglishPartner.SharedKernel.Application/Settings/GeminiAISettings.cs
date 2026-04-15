namespace EnglishPartner.SharedKernel.Settings;

public sealed class GeminiAISettings
{
    public const string SectionName = "GeminiAI";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models";
    public string ModelName { get; set; } = "gemini-3.1-flash-lite-preview";
    public string? GenerateContentApi { get; set; } = "generateContent";
    public int MaxOutputTokens { get; set; } = 8192;
    public double Temperature { get; set; } = 0.7;
    public double TopP { get; set; } = 0.95;
    
    /// <summary>
    /// E.g. "HIGH", "LOW", or null to disable.
    /// </summary>
    public string? ThinkingLevel { get; set; }

    /// <summary>
    /// If true, disables all HARM categories (OFF).
    /// </summary>
    public bool DisableAllSafetyRestrictions { get; set; } = true;

    /// <summary>
    /// Timeout (in seconds) for a single Gemini API call.
    /// - Evaluation / Chat: typically 15–30s
    /// - Content Generation (grammar, vocabulary, conversation...): may need 60–120s
    /// Default: 120s — safe for bulk generation. Reduce for eval-only workloads.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;
}
