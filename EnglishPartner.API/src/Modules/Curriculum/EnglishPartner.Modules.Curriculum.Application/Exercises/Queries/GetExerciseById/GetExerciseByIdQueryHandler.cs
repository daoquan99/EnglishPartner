using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Exercises.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Exercises.Queries.GetExerciseById;

internal sealed class GetExerciseByIdQueryHandler(
    IExerciseRepository exerciseRepository) : IQueryHandler<GetExerciseByIdQuery, ExerciseDetailResponse>
{
    public async Task<Result<ExerciseDetailResponse>> Handle(GetExerciseByIdQuery request, CancellationToken cancellationToken)
    {
        var exercise = await exerciseRepository.GetByIdAsync(request.Id, cancellationToken);
        if (exercise is null) return Result.Failure<ExerciseDetailResponse>(CurriculumErrors.Exercise.NotFound(request.Id));

        return new ExerciseDetailResponse(
            exercise.Id,
            exercise.Title,
            exercise.Instruction,
            exercise.Type.ToString(),
            exercise.CorrectAnswer,
            exercise.LanguageCode,
            exercise.Level,
            exercise.TopicId,
            exercise.VocabularyId,
            exercise.GrammarId,
            exercise.Content,
            exercise.Explanation,
            exercise.XpReward,
            exercise.Options.Select(o => new ExerciseOptionDto(
                o.Id, o.Text, o.IsCorrect, o.SortOrder)).ToList());
    }
}
