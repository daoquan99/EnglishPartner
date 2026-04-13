using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Exercises.Queries.GetExerciseById;

public sealed record GetExerciseByIdQuery(Guid Id) : IQuery<ExerciseDetailResponse>;

public sealed record ExerciseDetailResponse(
    Guid Id,
    string Title,
    string Instruction,
    string Type,
    string CorrectAnswer,
    string? LanguageCode,
    string? Level,
    Guid? TopicId,
    Guid? VocabularyId,
    Guid? GrammarId,
    string? Content,
    string? Explanation,
    int XpReward,
    List<ExerciseOptionDto> Options);

public sealed record ExerciseOptionDto(
    Guid Id,
    string Text,
    bool IsCorrect,
    int SortOrder);
