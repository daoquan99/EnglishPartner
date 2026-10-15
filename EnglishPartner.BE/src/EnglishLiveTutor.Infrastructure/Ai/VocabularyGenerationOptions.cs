namespace EnglishLiveTutor.Infrastructure.Ai;

public sealed class VocabularyGenerationOptions
{
    public const string SectionName = "Ai:VocabularyGeneration";

    public int TimeoutSeconds { get; set; } = 300;
}
