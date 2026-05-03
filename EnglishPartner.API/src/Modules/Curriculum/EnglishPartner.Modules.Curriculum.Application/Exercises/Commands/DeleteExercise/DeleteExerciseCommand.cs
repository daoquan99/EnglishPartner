using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Exercises.Commands.DeleteExercise;

public sealed record DeleteExerciseCommand(Guid Id) : ICommand;
