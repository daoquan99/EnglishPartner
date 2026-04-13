namespace EnglishPartner.SharedKernel.Settings;

public sealed class GeminiAISettings
{
    public const string SectionName = "GeminiAI";

    public string ApiKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = "gemini-3.1-flash-lite-preview";
    public int MaxTokens { get; set; } = 8192;
    public double Temperature { get; set; } = 0.7;
}
