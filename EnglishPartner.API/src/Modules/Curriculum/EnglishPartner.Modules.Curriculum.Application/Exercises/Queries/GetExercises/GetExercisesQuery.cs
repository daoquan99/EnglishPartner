using EnglishPartner.SharedKernel.Application.Caching;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Exercises.Queries.GetExercises;

public sealed record GetExercisesQuery(
    string LanguageCode,
    int Page = 1,
    int PageSize = 20,
    Guid? TopicId = null,
    Guid? VocabularyId = null,
    Guid? GrammarId = null,
    string? Level = null,
    int? ExerciseType = null) : ICacheableQuery<ExercisesResponse>
{
    public string CacheKey   => CacheKeys.Curriculum.ExercisesQuery(LanguageCode, Page, PageSize, TopicId, VocabularyId, GrammarId, Level, ExerciseType);
    public TimeSpan? Expiration => TimeSpan.FromHours(6);
}

public sealed record ExercisesResponse(
    List<ExerciseListItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ExerciseListItemResponse(
    Guid Id,
    string Title,
    string Instruction,
    string Type,
    string? Level,
    int XpReward);
