using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Exercises.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Exercises.Commands.AddExerciseOption;

internal sealed class AddExerciseOptionCommandHandler(
    IExerciseRepository exerciseRepository) : ICommandHandler<AddExerciseOptionCommand>
{
    public async Task<Result> Handle(AddExerciseOptionCommand request, CancellationToken cancellationToken)
    {
        var exercise = await exerciseRepository.GetByIdAsync(request.ExerciseId, cancellationToken);
        if (exercise is null) return Result.Failure(CurriculumErrors.Exercise.NotFound(request.ExerciseId));

        exercise.AddOption(request.Text, request.IsCorrect);
        
        exerciseRepository.Update(exercise);
        return Result.Success();
    }
}
