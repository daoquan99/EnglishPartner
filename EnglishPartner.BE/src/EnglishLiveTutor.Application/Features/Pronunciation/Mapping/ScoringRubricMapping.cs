using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Domain.Pronunciation.Assessments;
using EnglishLiveTutor.Domain.Pronunciation.ScoringRubrics;
using EnglishLiveTutor.Domain.Pronunciation.AudioRecordings;
using EnglishLiveTutor.Domain.Pronunciation.GeneratedAudio;
using EnglishLiveTutor.Domain.Pronunciation.Assessments.Entities;
using EnglishLiveTutor.Domain.Pronunciation.ScoringRubrics.Entities;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Mapping;

public static class ScoringRubricMapping
{
    public static ScoringRubricDto ToDto(this ScoringRubricEntity entity) => new(
        Id: entity.Id,
        Code: entity.Code,
        Name: entity.Name,
        Description: entity.Description,
        Version: entity.Version,
        AssessmentType: entity.AssessmentType,
        IsActive: entity.IsActive,
        CreatedAtUtc: entity.CreatedAtUtc,
        CreatedBy: entity.CreatedBy,
        UpdatedAtUtc: entity.UpdatedAtUtc,
        UpdatedBy: entity.UpdatedBy,
        Criteria: entity.Criteria
            .OrderBy(c => c.DisplayOrder)
            .Select(c => c.ToDto())
            .ToList()
    );

    public static ScoringRubricCriterionDto ToDto(this ScoringRubricCriterionEntity entity) => new(
        Id: entity.Id,
        RubricId: entity.RubricId,
        Code: entity.Code,
        Name: entity.Name,
        Description: entity.Description,
        Weight: entity.Weight,
        MinScore: entity.MinScore,
        MaxScore: entity.MaxScore,
        DisplayOrder: entity.DisplayOrder,
        IsEnabled: entity.IsEnabled
    );
}
