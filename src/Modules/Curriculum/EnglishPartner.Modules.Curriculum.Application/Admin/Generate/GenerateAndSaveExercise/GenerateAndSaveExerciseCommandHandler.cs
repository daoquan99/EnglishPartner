using EnglishPartner.Modules.AI.Contracts.Services;
using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Exercises;
using EnglishPartner.Modules.Curriculum.Domain.Exercises.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateAndSaveExercise;

internal sealed class GenerateAndSaveExerciseCommandHandler(
    ICurriculumContentGenerationService generationService,
    IExerciseRepository exerciseRepository) : ICommandHandler<GenerateAndSaveExerciseCommand, GenerateAndSaveExerciseResponse>
{
    /// <summary>Exercise types that AI can meaningfully generate (text-only, no media required).</summary>
    private static readonly HashSet<ExerciseType> SupportedTypes =
    [
        ExerciseType.MultipleChoice,
        ExerciseType.FillInTheBlank,
        ExerciseType.TrueFalse,
        ExerciseType.Translation,
        ExerciseType.SentenceCompletion,
        ExerciseType.WordForm,
        ExerciseType.VerbTense,
        ExerciseType.Paraphrasing,
        ExerciseType.GapFilling,
        ExerciseType.ReorderingWords,
        ExerciseType.DialogueCompletion,
        ExerciseType.ShortAnswerQuestions,
        ExerciseType.TrueFalseNotGiven,
        ExerciseType.RewritingSentences,
    ];

    public async Task<Result<GenerateAndSaveExerciseResponse>> Handle(
        GenerateAndSaveExerciseCommand request, CancellationToken cancellationToken)
    {
        if (!SupportedTypes.Contains(request.ExerciseType))
            return Result.Failure<GenerateAndSaveExerciseResponse>(
                CurriculumErrors.Exercise.UnsupportedAiType(request.ExerciseType.ToString()));

        var count = Math.Clamp(request.Count, 1, 20);
        var generated = await generationService.GenerateExerciseAsync(
            request.TargetLanguage, request.Context, (int)request.ExerciseType, request.Level, count, cancellationToken);

        var items = new List<GeneratedExerciseItem>();

        foreach (var dto in generated)
        {
            var exercise = Exercise.Create(
                dto.Title,
                dto.Instruction,
                request.ExerciseType,
                dto.CorrectAnswer,
                languageCode: request.TargetLanguage,
                request.Level,
                request.TopicId,
                request.VocabularyId,
                request.GrammarId,
                dto.Content,
                dto.Explanation,
                request.XpReward,
                isAiGenerated: true);

            foreach (var option in dto.Options)
                exercise.AddOption(option.Text, option.IsCorrect);

            exerciseRepository.Add(exercise);

            items.Add(new GeneratedExerciseItem(
                exercise.Id,
                exercise.Title,
                exercise.Instruction,
                exercise.Content,
                exercise.CorrectAnswer,
                exercise.Explanation,
                exercise.Type,
                exercise.IsAiGenerated,
                exercise.Options
                    .Select(o => new GeneratedExerciseOptionItem(o.Text, o.IsCorrect))
                    .ToList()));
        }

        return new GenerateAndSaveExerciseResponse(items);
    }
}
