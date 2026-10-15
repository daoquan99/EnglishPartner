using EnglishLiveTutor.Application.Common.Interfaces;

namespace EnglishLiveTutor.Application.Features.Learning.Queries.GetLearningHistory;

public sealed record LearningHistoryItemDto(
    Guid Id,
    Guid VocabularyId,
    Guid? SenseId,
    string Word,
    string ReviewType,
    string Rating,
    DateTimeOffset ReviewedAt,
    DateTimeOffset? NextReviewAtSnapshot
);

public sealed record GetLearningHistoryQuery(string UserId, int Limit = 30) : IQuery<IReadOnlyList<LearningHistoryItemDto>>;
