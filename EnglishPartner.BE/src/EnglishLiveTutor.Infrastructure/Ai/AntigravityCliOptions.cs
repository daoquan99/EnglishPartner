namespace EnglishLiveTutor.Infrastructure.Ai;

public sealed class AntigravityCliOptions
{
    public const string SectionName = "Ai:AntigravityCli";

    public string? ExecutablePath { get; set; }
    public string DefaultModel { get; set; } = "gemini-3.8-flash-medium";
    public int TimeoutSeconds { get; set; } = 120;
    public List<string> AllowedModels { get; set; } = new()
    {
        "gemini-3.8-flash-medium",
        "gemini-3.8-flash-low",
        "gemini-3.8-flash-high"
    };
}
