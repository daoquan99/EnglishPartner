using EnglishPartner.Modules.Curriculum.Contracts.IntegrationEvents;
using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Exercises;
using EnglishPartner.Modules.Curriculum.Domain.Exercises.Repositories;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.Messaging;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Exercises.Commands.SubmitExercise;

internal sealed class SubmitExerciseCommandHandler(
    IExerciseRepository exerciseRepository,
    ICurrentUserService currentUserService,
    IEventBus eventBus) : ICommandHandler<SubmitExerciseCommand>
{
    public async Task<Result> Handle(SubmitExerciseCommand request, CancellationToken cancellationToken)
    {
        // Only grant XP if the answer is correct
        if (!request.IsCorrect)
        {
            return Result.Success();
        }

        var exercise = await exerciseRepository.GetByIdAsync(request.ExerciseId, cancellationToken);
        if (exercise is null)
        {
            return Result.Failure(CurriculumErrors.Exercise.NotFound(request.ExerciseId));
        }

        if (exercise.XpReward > 0 && currentUserService.UserId.HasValue)
        {
            await eventBus.PublishAsync(
                new ExerciseCompletedIntegrationEvent(currentUserService.UserId.Value, exercise.Id, exercise.XpReward),
                cancellationToken);
        }

        return Result.Success();
    }
}
