namespace EnglishLiveTutor.Application.Features.Vocabulary.Contracts;

public sealed record GenerateVocabularyRequest(
    string ApiKey,
    string ModelCode = "gemini-3.8-flash",
    int RequestedCount = 5,
    string CefrLevel = "B1",
    string? Topic = null,
    List<string>? ExcludedWords = null,
    bool AssignToTodayPlan = true
);

public sealed record BuildVocabularyGenerationPromptRequest(
    int RequestedCount = 5,
    string CefrLevel = "B1",
    string? Topic = null,
    List<string>? ExcludedWords = null
);

public sealed record VocabularyGenerationPromptDto(
    string Prompt,
    string PromptVersion,
    int RequestedCount,
    string CefrLevel,
    string Topic,
    IReadOnlyList<string> ExcludedWords
);

public sealed record ImportVocabularyJsonRequest(
    string JsonText,
    string ModelCode = "manual-web-ai",
    string CefrLevel = "B1",
    string? Topic = null,
    bool AssignToTodayPlan = true
);

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
