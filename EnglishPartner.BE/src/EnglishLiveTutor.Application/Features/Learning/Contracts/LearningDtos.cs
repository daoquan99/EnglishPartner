using EnglishLiveTutor.Application.Features.Vocabulary.Contracts;

namespace EnglishLiveTutor.Application.Features.Learning.Contracts;

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

public sealed record SubmitVocabularyReviewRequest(
    string Rating,
    string TimeZoneId = "Asia/Ho_Chi_Minh"
);

public sealed record LearningProgressSummaryDto(
    string UserId,
    int TotalCatalogSenses,
    int TotalStudied,
    int MasteredCount,
    int ReviewCount,
    int LearningCount,
    int DueForReviewNow,
    double AverageMastery,
    int TotalPronunciationAssessments,
    decimal? AveragePronunciationScore
);

public sealed record LearningHistoryItemDto(
    Guid Id,
    Guid VocabularyId,
    Guid SenseId,
    string Word,
    string ReviewType,
    string Rating,
    DateTimeOffset ReviewedAt,
    DateTimeOffset? NextReviewAtSnapshot
);
