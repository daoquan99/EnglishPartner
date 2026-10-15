namespace EnglishLiveTutor.Application.Features.Vocabulary.Contracts;

public sealed record PronunciationVariantDto(
    Guid Id,
    string Accent,
    string Ipa,
    string? AudioUrl,
    string AudioSource,
    int? SyllableCount,
    string? StressPattern
);

public sealed record WordFamilyItemDto(
    Guid Id,
    string RelatedWord,
    string PartOfSpeech,
    Guid? RelatedVocabularyId
);

public sealed record CollocationItemDto(
    Guid Id,
    string PhraseEn,
    string? MeaningVi,
    int DisplayOrder
);

public sealed record ExampleSentenceDto(
    Guid Id,
    string SentenceEn,
    string? SentenceVi,
    string? AudioUrl,
    string? CefrLevel,
    int DisplayOrder
);

public sealed record GrammarPatternDto(
    Guid Id,
    string Pattern,
    string? Explanation,
    string? Example
);

public sealed record VocabularyFlashcardDto(
    Guid DailyItemId,
    Guid VocabularyId,
    Guid SenseId,
    string Word,
    string PartOfSpeech,
    string CefrLevel,
    string DefinitionEn,
    string MeaningVi,
    string? DescriptionEn,
    List<PronunciationVariantDto> Pronunciations,
    List<string> Synonyms,
    List<string> Antonyms,
    List<WordFamilyItemDto> WordFamilies,
    List<CollocationItemDto> Collocations,
    List<ExampleSentenceDto> Examples,
    List<GrammarPatternDto> GrammarPatterns,
    string ItemType,
    string ItemStatus,
    string LearningStatus,
    int MasteryLevel,
    bool IsNew,
    bool IsDueForReview,
    decimal? LatestPronunciationScore,
    DateTimeOffset? NextReviewAt,
    int IntervalDays,
    decimal EaseFactor,
    string? TopicCategory = null,
    int? FrequencyRank = null
);

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

public sealed record UpsertVocabularyAdminRequest(
    string Word,
    string PartOfSpeech = "noun",
    string CefrLevel = "B1",
    string DefinitionEn = "",
    string MeaningVi = "",
    string DescriptionEn = "",
    string? TopicCategory = null,
    int? FrequencyRank = null,
    string UkIpa = "",
    string UsIpa = "",
    string? StressPattern = null,
    List<string>? Synonyms = null,
    List<string>? Antonyms = null,
    List<UpsertWordFamilyItem>? WordFamilies = null,
    List<UpsertCollocationItem>? Collocations = null,
    List<UpsertExampleItem>? Examples = null,
    List<UpsertGrammarPatternItem>? GrammarPatterns = null
);

public sealed record UpsertWordFamilyItem(string WordForm, string PartOfSpeech, string? Note = null);
public sealed record UpsertCollocationItem(string Phrase, string MeaningVi, string? ExampleSentence = null);
public sealed record UpsertExampleItem(string SentenceEn, string TranslationVi, string CefrLevel = "B1");
public sealed record UpsertGrammarPatternItem(string Pattern, string ExplanationVi, string? SampleSentence = null);

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
