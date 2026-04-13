using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Exercises.Commands.AddExerciseOption;

public sealed record AddExerciseOptionCommand(
    Guid ExerciseId,
    string Text,
    bool IsCorrect) : ICommand;
