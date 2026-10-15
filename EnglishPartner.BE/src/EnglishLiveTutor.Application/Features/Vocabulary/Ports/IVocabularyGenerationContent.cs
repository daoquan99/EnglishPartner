using EnglishLiveTutor.Application.Features.Vocabulary.Contracts;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Ports;

public interface IVocabularyGenerationContent
{
    string BuildPrompt(string cefrLevel, string topic, int count, IReadOnlyList<string> excludedWords);
    List<GeneratedVocabularyRawSense> ParseResponse(string jsonText, string cefrLevel);
}
