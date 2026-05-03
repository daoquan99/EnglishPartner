using EnglishPartner.Modules.Curriculum.Domain.Exercises;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Exercises.Commands.CreateExercise;

public sealed record CreateExerciseCommand(
    string Title,
    string Instruction,
    ExerciseType Type,
    string CorrectAnswer,
    string LanguageCode,
    string? Level,
    Guid? TopicId,
    Guid? VocabularyId,
    Guid? GrammarId,
    string? Content,
    string? Explanation,
    int XpReward) : ICommand<Guid>;
