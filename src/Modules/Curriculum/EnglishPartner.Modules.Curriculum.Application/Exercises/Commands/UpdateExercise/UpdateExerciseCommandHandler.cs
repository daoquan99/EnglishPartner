using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Exercises.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Exercises.Commands.UpdateExercise;

internal sealed class UpdateExerciseCommandHandler(
    IExerciseRepository exerciseRepository) : ICommandHandler<UpdateExerciseCommand>
{
    public async Task<Result> Handle(UpdateExerciseCommand request, CancellationToken cancellationToken)
    {
        var exercise = await exerciseRepository.GetByIdAsync(request.Id, cancellationToken);
        if (exercise is null) return Result.Failure(CurriculumErrors.Exercise.NotFound(request.Id));

        exercise.Update(
            request.Title,
            request.Instruction,
            request.Type,
            request.CorrectAnswer,
            request.Level,
            request.TopicId,
            request.VocabularyId,
            request.GrammarId,
            request.Content,
            request.Explanation,
            request.XpReward);

        exerciseRepository.Update(exercise);
        return Result.Success();
    }
}
