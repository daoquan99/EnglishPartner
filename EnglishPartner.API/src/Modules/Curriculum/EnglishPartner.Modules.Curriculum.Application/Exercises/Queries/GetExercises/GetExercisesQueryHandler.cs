using EnglishPartner.Modules.Curriculum.Domain.Exercises.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Exercises.Queries.GetExercises;

internal sealed class GetExercisesQueryHandler(
    IExerciseRepository exerciseRepository) : IQueryHandler<GetExercisesQuery, ExercisesResponse>
{
    public async Task<Result<ExercisesResponse>> Handle(GetExercisesQuery request, CancellationToken cancellationToken)
    {
        var exerciseType = request.ExerciseType.HasValue
            ? (Domain.Exercises.ExerciseType)request.ExerciseType.Value
            : (Domain.Exercises.ExerciseType?)null;

        var (items, totalCount) = await exerciseRepository.GetPagedAsync(
            request.LanguageCode, request.Page, request.PageSize,
            request.TopicId, request.VocabularyId, request.GrammarId, request.Level, exerciseType, cancellationToken);

        var dtos = items.Select(e => new ExerciseListItemResponse(
            e.Id, e.Title, e.Instruction, e.Type.ToString(), e.Level, e.XpReward)).ToList();

        return new ExercisesResponse(dtos, totalCount, request.Page, request.PageSize);
    }
}
