using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateExerciseOptions;

/// <summary>
/// Admin command: Ask AI to generate answer options for an existing exercise.
/// Clears existing options and replaces them with AI-generated ones.
/// </summary>
public sealed record GenerateExerciseOptionsCommand(
    Guid ExerciseId,
    int OptionCount = 4) : ICommand<GenerateExerciseOptionsResponse>;

public sealed record GenerateExerciseOptionsResponse(
    Guid ExerciseId,
    List<GeneratedOptionItem> Options);

public sealed record GeneratedOptionItem(
    string Text,
    bool IsCorrect);
