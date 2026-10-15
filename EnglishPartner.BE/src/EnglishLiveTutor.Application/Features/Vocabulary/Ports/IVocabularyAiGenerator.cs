using EnglishLiveTutor.Application.Features.Vocabulary.Contracts;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Ports;

public interface IVocabularyAiGenerator
{
    Task<List<GeneratedVocabularyRawSense>> GenerateVocabularyAsync(
        string apiKey,
        string modelCode,
        string cefrLevel,
        string topic,
        int count,
        List<string> excludedWords,
        CancellationToken ct = default);
}
