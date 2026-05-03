using EnglishPartner.Modules.Curriculum.Domain.Exercises;
using EnglishPartner.Modules.Curriculum.Domain.Exercises.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Exercises.Commands.CreateExercise;

internal sealed class CreateExerciseCommandHandler(
    IExerciseRepository exerciseRepository) : ICommandHandler<CreateExerciseCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateExerciseCommand request, CancellationToken cancellationToken)
    {
        var exercise = Exercise.Create(
            request.Title,
            request.Instruction,
            request.Type,
            request.CorrectAnswer,
            request.LanguageCode,
            request.Level,
            request.TopicId,
            request.VocabularyId,
            request.GrammarId,
            request.Content,
            request.Explanation,
            request.XpReward);

        exerciseRepository.Add(exercise);
        return exercise.Id;
    }
}
