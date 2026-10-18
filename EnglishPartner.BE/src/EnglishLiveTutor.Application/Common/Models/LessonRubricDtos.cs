namespace EnglishLiveTutor.Application.Common.Models;

public sealed record RubricCriterionDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    decimal Weight,
    int DisplayOrder,
    string DescriptorLevel0,
    string DescriptorLevel1,
    string DescriptorLevel2,
    string DescriptorLevel3,
    string DescriptorLevel4
);

public sealed record LessonRubricSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    int Version,
    string AssessmentScope,
    string? ExerciseType,
    string? SubQuestionKind,
    string CefrLevel,
    List<string>? ApplicableExerciseTypes,
    bool IsActive,
    int CriteriaCount
);

public sealed record LessonRubricDetailDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    int Version,
    string AssessmentScope,
    string? ExerciseType,
    string? SubQuestionKind,
    string CefrLevel,
    List<string>? ApplicableExerciseTypes,
    bool IsActive,
    IReadOnlyList<RubricCriterionDto> Criteria
);

public sealed record CreateLessonRubricRequest(
    string Code,
    string Name,
    string Description,
    string? ExerciseType,
    string? SubQuestionKind,
    string CefrLevel,
    List<string>? ApplicableExerciseTypes,
    List<CreateRubricCriterionRequest> Criteria
);

public sealed record CreateRubricCriterionRequest(
    string Code,
    string Name,
    string Description,
    decimal Weight,
    int DisplayOrder,
    string? DescriptorLevel0,
    string? DescriptorLevel1,
    string? DescriptorLevel2,
    string? DescriptorLevel3,
    string? DescriptorLevel4
);
