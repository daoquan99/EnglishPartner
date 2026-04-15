using EnglishPartner.SharedKernel.Application.Caching;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Exercises.Queries.GetExercises;

public sealed record GetExercisesQuery(
    string LanguageCode,
    Guid? TopicId = null,
    Guid? VocabularyId = null,
    Guid? GrammarId = null) : ICacheableQuery<List<ExerciseListItemResponse>>
{
    public string CacheKey   => $"Exercises:{LanguageCode}:{TopicId}:{VocabularyId}:{GrammarId}";
    public TimeSpan? Expiration => TimeSpan.FromHours(6);
}

public sealed record ExerciseListItemResponse(
    Guid Id,
    string Title,
    string Type,
    string? Level,
    int XpReward);
