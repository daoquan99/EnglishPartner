namespace EnglishLiveTutor.Application.Common.Models;

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
    decimal EaseFactor
);

public sealed record DailyVocabularyPlanResponseDto(
    Guid PlanId,
    string UserId,
    string LearningDate,
    string TimeZoneId,
    int NewVocabularyTarget,
    int TotalAssignedCount,
    int NewCount,
    int PendingReviewCount,
    int CarryOverCount,
    int CompletedCount,
    string Status,
    List<VocabularyFlashcardDto> Flashcards
);

public sealed record InitializeDailyPlanRequest(
    string? ApiKey,
    string? GenerationModelCode = "gemini-3.8-flash",
    string CefrLevel = "B1",
    int NewVocabularyTarget = 20,
    string TimeZoneId = "Asia/Ho_Chi_Minh",
    string? Topic = null,
    bool GenerateWithAiIfNeeded = false
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

public sealed record SubmitVocabularyReviewRequest(
    string Rating, // Again | Hard | Good | Easy
    string TimeZoneId = "Asia/Ho_Chi_Minh"
);

public sealed record SubmitPronunciationAssessmentRequest(
    string ApiKey,
    Guid VocabularyId,
    Guid SenseId,
    string TargetType, // Word | Sentence
    Guid? TargetId,
    string? ReferenceText,
    string Accent, // US | UK
    string AssessmentModelCode,
    string Base64Audio,
    string ContentType = "audio/webm",
    int DurationMilliseconds = 2500
);

public sealed record CriterionScoreResultDto(
    string Code,
    string Name,
    string Description,
    decimal Weight,
    decimal Score,
    decimal WeightedScore,
    string Feedback
);

public sealed record PronunciationAssessmentResultDto(
    Guid AssessmentId,
    Guid VocabularyId,
    Guid SenseId,
    string TargetType,
    Guid? TargetId,
    string ExpectedText,
    string? RecognizedText,
    string Accent,
    string ModelCode,
    string RubricCode,
    int RubricVersion,
    decimal? OverallScore,
    decimal? Confidence,
    bool IsAssessable,
    string Status,
    List<CriterionScoreResultDto> Criteria,
    List<string> Strengths,
    List<string> Improvements,
    List<string> SuggestedPractice,
    DateTimeOffset CreatedAt
);

public sealed record UpdateRubricCriterionRequest(
    string Code,
    string Name,
    string Description,
    decimal Weight
);

public sealed record UpdateScoringRubricRequest(
    string Name,
    string Description,
    List<UpdateRubricCriterionRequest> Criteria
);
