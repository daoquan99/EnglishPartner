namespace EnglishLiveTutor.Infrastructure.Ai;

public sealed class GrammarGenerationAiOptions
{
    public const string SectionName = "Ai:GrammarGeneration";

    public int TimeoutSeconds { get; set; } = 600;
}
