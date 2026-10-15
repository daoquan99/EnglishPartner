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
    string ItemType, // New | Review | CarryOver
    string ItemStatus, // Pending | InProgress | Completed
    string LearningStatus, // New | Learning | Review | Mastered
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
