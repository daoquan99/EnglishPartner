using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Exercises.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Exercises.Commands.DeleteExercise;

internal sealed class DeleteExerciseCommandHandler(
    IExerciseRepository exerciseRepository,
    ICurrentUserService currentUser,
    TimeProvider timeProvider) : ICommandHandler<DeleteExerciseCommand>
{
    public async Task<Result> Handle(DeleteExerciseCommand request, CancellationToken cancellationToken)
    {
        var exercise = await exerciseRepository.GetByIdAsync(request.Id, cancellationToken);
        if (exercise is null) return Result.Failure(CurriculumErrors.Exercise.NotFound(request.Id));

        exerciseRepository.Remove(exercise, currentUser.UserId, timeProvider.GetUtcNow().UtcDateTime);
        return Result.Success();
    }
}
