using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Domain.Pronunciation.ScoringRubrics;
using EnglishLiveTutor.Domain.Pronunciation.ScoringRubrics.Entities;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Mapping;

public static class PronunciationMapping
{
    public static ScoringRubricCriterionDto ToDto(this ScoringRubricCriterionEntity c) =>
        new(
            Id: c.Id,
            RubricId: c.RubricId,
            Code: c.Code,
            Name: c.Name,
            Description: c.Description,
            Weight: c.Weight,
            MinimumScore: c.MinimumScore,
            MaximumScore: c.MaximumScore,
            DisplayOrder: c.DisplayOrder,
            IsEnabled: c.IsEnabled,
            CreatedAtUtc: c.CreatedAtUtc,
            CreatedBy: c.CreatedBy,
            UpdatedAtUtc: c.UpdatedAtUtc,
            UpdatedBy: c.UpdatedBy,
            IsDeleted: c.IsDeleted,
            DeletedAtUtc: c.DeletedAtUtc,
            DeletedBy: c.DeletedBy,
            CreatedAt: c.CreatedAt,
            UpdatedAt: c.UpdatedAt
        );

    public static ScoringRubricDto ToDto(this ScoringRubricEntity r) =>
        new(
            Id: r.Id,
            Code: r.Code,
            Name: r.Name,
            Description: r.Description,
            Version: r.Version,
            AssessmentType: r.AssessmentType,
            IsActive: r.IsActive,
            Criteria: r.Criteria
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.DisplayOrder)
                .Select(ToDto)
                .ToList(),
            CreatedAtUtc: r.CreatedAtUtc,
            CreatedBy: r.CreatedBy,
            UpdatedAtUtc: r.UpdatedAtUtc,
            UpdatedBy: r.UpdatedBy,
            IsDeleted: r.IsDeleted,
            DeletedAtUtc: r.DeletedAtUtc,
            DeletedBy: r.DeletedBy,
            CreatedAt: r.CreatedAt,
            UpdatedAt: r.UpdatedAt
        );
}
