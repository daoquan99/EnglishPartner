using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Exercises.Commands.SubmitExercise;

public sealed record SubmitExerciseCommand(Guid ExerciseId, bool IsCorrect) : ICommand;
