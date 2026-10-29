using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Pronunciation;

namespace EnglishLiveTutor.Application.Common.Interfaces;

public sealed record GeneratedVocabularyRawSense(
    string Word,
    string PartOfSpeech,
    string CefrLevel,
    string DefinitionEn,
    string MeaningVi,
    string? DescriptionEn,
    string? IpaUs,
    string? IpaUk,
    List<string> Synonyms,
    List<string> Antonyms,
    List<GeneratedWordFamilyRaw> WordFamilies,
    List<GeneratedCollocationRaw> Collocations,
    List<GeneratedExampleRaw> Examples,
    List<GeneratedGrammarPatternRaw> GrammarPatterns
);

public sealed record GeneratedWordFamilyRaw(string RelatedWord, string PartOfSpeech);
public sealed record GeneratedCollocationRaw(string PhraseEn, string MeaningVi);
public sealed record GeneratedExampleRaw(string SentenceEn, string SentenceVi);
public sealed record GeneratedGrammarPatternRaw(string Pattern, string Explanation, string Example);

public sealed record VocabularyGenerationRequestOptions(
    string ApiKey,
    string ModelCode,
    string CefrLevel,
    string Topic,
    int Count,
    IReadOnlyList<string> ExcludedWords,
    string? Provider = null,
    int ExamplesPerSense = 2,
    IReadOnlyList<string>? TargetTerms = null,
    string? SourceSentence = null
);

public interface IVocabularyAiGenerator
{
    Task<List<GeneratedVocabularyRawSense>> GenerateVocabularyAsync(
        VocabularyGenerationRequestOptions options,
        CancellationToken ct = default);
}

public sealed record PronunciationAiRawResult(
    string? RecognizedText,
    decimal? Confidence,
    bool IsAssessable,
    Dictionary<string, (decimal RawScore, string Feedback)> CriterionScores,
    List<string> Strengths,
    List<string> Improvements,
    List<string> SuggestedPractice
);

public interface IPronunciationAiAssessor
{
    Task<PronunciationAiRawResult> AssessAudioAsync(
        string apiKey,
        string modelCode,
        string base64Audio,
        string contentType,
        string expectedText,
        string accent,
        ScoringRubricEntity rubric,
        CancellationToken ct = default);
}

public interface ILivePronunciationTtsSynthesizer
{
    Task<SynthesizePronunciationAudioResponseDto> SynthesizeSpeechAsync(
        SynthesizePronunciationAudioRequest request,
        CancellationToken ct = default);
}
