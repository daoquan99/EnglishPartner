using EnglishPartner.Modules.AI.Contracts.Services;
using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Exercises;
using EnglishPartner.Modules.Curriculum.Domain.Exercises.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateExerciseOptions;

internal sealed class GenerateExerciseOptionsCommandHandler(
    ICurriculumContentGenerationService generationService,
    IExerciseRepository exerciseRepository) : ICommandHandler<GenerateExerciseOptionsCommand, GenerateExerciseOptionsResponse>
{
    public async Task<Result<GenerateExerciseOptionsResponse>> Handle(
        GenerateExerciseOptionsCommand request, CancellationToken cancellationToken)
    {
        var exercise = await exerciseRepository.GetByIdAsync(request.ExerciseId, cancellationToken);
        if (exercise is null)
            return Result.Failure<GenerateExerciseOptionsResponse>(CurriculumErrors.Exercise.NotFound(request.ExerciseId));

        var options = await generationService.GenerateExerciseOptionsAsync(
            exercise.LanguageCode,
            (int)exercise.Type,
            exercise.Title,
            exercise.Instruction,
            exercise.Content,
            exercise.CorrectAnswer,
            request.OptionCount,
            cancellationToken);

        exercise.ClearOptions();
        foreach (var option in options)
            exercise.AddOption(option.Text, option.IsCorrect);

        exerciseRepository.Update(exercise);

        return new GenerateExerciseOptionsResponse(
            exercise.Id,
            exercise.Options.Select(o => new GeneratedOptionItem(o.Text, o.IsCorrect)).ToList());
    }
}
