using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Exercises.Queries.GetExercises;

public sealed record GetExercisesQuery(
    string LanguageCode,
    Guid? TopicId = null,
    Guid? VocabularyId = null,
    Guid? GrammarId = null) : IQuery<List<ExerciseListItemResponse>>;

public sealed record ExerciseListItemResponse(
    Guid Id,
    string Title,
    string Type,
    string? Level,
    int XpReward);
