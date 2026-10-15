namespace EnglishLiveTutor.Application.Features.Vocabulary;

public static class VocabularyGenerationDefaults
{
    public const string PromptVersion = "PROMPT_VOCAB_GEN_V1";
    public const string Topic = "General English & Career Communication";
    public const int MaxCount = 10;

    public static string NormalizeCefr(string? value)
    {
        string normalized = string.IsNullOrWhiteSpace(value) ? "B1" : value.Trim().ToUpperInvariant();
        if (normalized is not ("A1" or "A2" or "B1" or "B2" or "C1" or "C2"))
            throw new InvalidOperationException("CEFR phải là A1, A2, B1, B2, C1 hoặc C2.");
        return normalized;
    }

    public static string NormalizeTopic(string? value)
    {
        string topic = string.IsNullOrWhiteSpace(value) ? Topic : value.Trim();
        if (topic.Length > 128)
            throw new InvalidOperationException("Chủ đề không được dài quá 128 ký tự.");
        return topic;
    }
}
