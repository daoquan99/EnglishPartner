namespace EnglishLiveTutor.Application.Common.Interfaces;

public sealed record VocabularyPromptOptions(
    string CefrLevel,
    string Topic,
    int Count,
    IReadOnlyList<string> ExcludedWords,
    int ExamplesPerSense = 2,
    IReadOnlyList<string>? TargetTerms = null,
    string? SourceSentence = null
);

public interface IVocabularyGenerationContent
{
    string BuildPrompt(VocabularyPromptOptions options);

    List<GeneratedVocabularyRawSense> ParseResponse(
        string jsonText,
        string cefrLevel,
        int examplesPerSense = 5,
        IReadOnlyList<string>? targetTerms = null);
}
