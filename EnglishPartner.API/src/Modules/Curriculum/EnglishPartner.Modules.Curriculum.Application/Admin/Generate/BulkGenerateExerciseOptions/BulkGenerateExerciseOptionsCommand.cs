using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Admin.Generate.BulkGenerateExerciseOptions;

/// <summary>
/// Admin command: Scans all exercises in DB, finds ones that require options
/// but have none, then calls AI to generate options for each.
/// </summary>
public sealed record BulkGenerateExerciseOptionsCommand : ICommand<BulkGenerateExerciseOptionsResponse>;

public sealed record BulkGenerateExerciseOptionsResponse(
    int TotalScanned,
    int OptionsGenerated,
    List<BulkGeneratedOptionItem> Items);

public sealed record BulkGeneratedOptionItem(
    Guid ExerciseId,
    string Title,
    int OptionCount);
