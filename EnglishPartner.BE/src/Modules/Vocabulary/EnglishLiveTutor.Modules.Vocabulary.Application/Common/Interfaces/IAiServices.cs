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
