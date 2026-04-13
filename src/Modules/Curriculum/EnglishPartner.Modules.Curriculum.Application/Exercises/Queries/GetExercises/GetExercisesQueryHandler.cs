using EnglishPartner.Modules.Curriculum.Domain.Exercises.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Exercises.Queries.GetExercises;

internal sealed class GetExercisesQueryHandler(
    IExerciseRepository exerciseRepository) : IQueryHandler<GetExercisesQuery, List<ExerciseListItemResponse>>
{
    public async Task<Result<List<ExerciseListItemResponse>>> Handle(GetExercisesQuery request, CancellationToken cancellationToken)
    {
        var exercises = new List<EnglishPartner.Modules.Curriculum.Domain.Exercises.Exercise>();

        if (request.TopicId.HasValue)
            exercises = await exerciseRepository.GetByTopicIdAsync(request.TopicId.Value, cancellationToken);
        else if (request.VocabularyId.HasValue)
            exercises = await exerciseRepository.GetByVocabularyIdAsync(request.VocabularyId.Value, cancellationToken);
        else if (request.GrammarId.HasValue)
            exercises = await exerciseRepository.GetByGrammarIdAsync(request.GrammarId.Value, cancellationToken);
        else
            exercises = await exerciseRepository.GetByLanguageAndLevelAsync(request.LanguageCode, null, 100, cancellationToken);

        return exercises.Select(e => new ExerciseListItemResponse(
            e.Id, e.Title, e.Type.ToString(), e.Level, e.XpReward)).ToList();
    }
}
