using EnglishPartner.Modules.AI.Contracts.Services;
using EnglishPartner.Modules.Curriculum.Domain.Exercises;
using EnglishPartner.Modules.Curriculum.Domain.Exercises.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Admin.Generate.BulkGenerateExerciseOptions;

internal sealed class BulkGenerateExerciseOptionsCommandHandler(
    ICurriculumContentGenerationService generationService,
    IExerciseRepository exerciseRepository)
    : ICommandHandler<BulkGenerateExerciseOptionsCommand, BulkGenerateExerciseOptionsResponse>
{
    public async Task<Result<BulkGenerateExerciseOptionsResponse>> Handle(
        BulkGenerateExerciseOptionsCommand request,
        CancellationToken cancellationToken)
    {
        var exercises = await exerciseRepository.GetWithoutOptionsAsync(cancellationToken);
        var needOptions = exercises
            .Where(e => e.Type.RequiresOptions())
            .ToList();

        var items = new List<BulkGeneratedOptionItem>();

        foreach (var exercise in needOptions)
        {
            var options = await generationService.GenerateExerciseOptionsAsync(
                targetLanguage: exercise.LanguageCode,
                exerciseType: (int)exercise.Type,
                title: exercise.Title,
                instruction: exercise.Instruction,
                content: exercise.Content,
                correctAnswer: exercise.CorrectAnswer,
                optionCount: exercise.Type.GetDefaultOptionCount(),
                cancellationToken: cancellationToken);

            exercise.ClearOptions();
            foreach (var option in options)
                exercise.AddOption(option.Text, option.IsCorrect);

            exerciseRepository.Update(exercise);

            items.Add(new BulkGeneratedOptionItem(
                ExerciseId: exercise.Id,
                Title: exercise.Title,
                OptionCount: options.Count));
        }

        return new BulkGenerateExerciseOptionsResponse(
            TotalScanned: exercises.Count,
            OptionsGenerated: items.Count,
            Items: items);
    }
}
