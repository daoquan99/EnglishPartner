namespace EnglishLiveTutor.Application.Common.Interfaces;

public interface IVocabularyGenerationContent
{
    string BuildPrompt(string cefrLevel, string topic, int count, IReadOnlyList<string> excludedWords);
    List<GeneratedVocabularyRawSense> ParseResponse(string jsonText, string cefrLevel);
}
