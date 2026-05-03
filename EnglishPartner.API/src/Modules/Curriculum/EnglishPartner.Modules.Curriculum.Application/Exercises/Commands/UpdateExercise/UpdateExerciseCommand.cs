using EnglishPartner.Modules.Curriculum.Domain.Exercises;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Exercises.Commands.UpdateExercise;

public sealed record UpdateExerciseCommand(
    Guid Id,
    string Title,
    string Instruction,
    ExerciseType Type,
    string CorrectAnswer,
    string? Level,
    Guid? TopicId,
    Guid? VocabularyId,
    Guid? GrammarId,
    string? Content,
    string? Explanation,
    int XpReward) : ICommand;
