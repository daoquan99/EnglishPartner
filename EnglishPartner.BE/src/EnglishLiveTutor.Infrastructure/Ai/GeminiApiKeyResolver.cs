namespace EnglishLiveTutor.Infrastructure.Ai;

public static class GeminiApiKeyResolver
{
    public static string? ResolveKey(string? requestKey, string? optionKey)
    {
        if (!string.IsNullOrWhiteSpace(requestKey))
        {
            return requestKey.Trim();
        }

        if (!string.IsNullOrWhiteSpace(optionKey))
        {
            return optionKey.Trim();
        }

        string? envKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? Environment.GetEnvironmentVariable("AI_STUDIO_KEY")
            ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY");

        if (!string.IsNullOrWhiteSpace(envKey))
        {
            return envKey.Trim();
        }

        return null;
    }
}
