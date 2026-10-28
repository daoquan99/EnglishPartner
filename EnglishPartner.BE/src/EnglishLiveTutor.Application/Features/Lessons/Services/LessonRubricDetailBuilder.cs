using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Lessons.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Lessons.Services;

public interface ILessonRubricDetailBuilder
{
    Task<LessonRubricDetailDto?> BuildDetailAsync(Guid rubricId, CancellationToken ct = default);
}

public sealed class LessonRubricDetailBuilder : ILessonRubricDetailBuilder
{
    private readonly IAppDbContext _db;

    public LessonRubricDetailBuilder(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<LessonRubricDetailDto?> BuildDetailAsync(Guid rubricId, CancellationToken ct = default)
    {
        var rubric = await _db.ScoringRubrics
            .AsNoTracking()
            .Include(r => r.Criteria)
            .FirstOrDefaultAsync(r => r.Id == rubricId, ct);

        if (rubric == null)
            return null;

        var criteriaDtos = rubric.Criteria
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new RubricCriterionDto(
                Id: c.Id,
                Code: c.Code,
                Name: c.Name,
                Description: c.Description,
                Weight: c.Weight,
                DisplayOrder: c.DisplayOrder,
                DescriptorLevel0: c.DescriptorLevel0,
                DescriptorLevel1: c.DescriptorLevel1,
                DescriptorLevel2: c.DescriptorLevel2,
                DescriptorLevel3: c.DescriptorLevel3,
                DescriptorLevel4: c.DescriptorLevel4
            )).ToList();

        return new LessonRubricDetailDto(
            Id: rubric.Id,
            Code: rubric.Code,
            Name: rubric.Name,
            Description: rubric.Description,
            Version: rubric.Version,
            AssessmentScope: rubric.AssessmentScope,
            ExerciseType: rubric.ExerciseType.HasValue ? ((ExerciseType)rubric.ExerciseType.Value).ToString() : null,
            SubQuestionKind: rubric.SubQuestionKind,
            CefrLevel: rubric.CefrLevel,
            ApplicableExerciseTypes: rubric.ApplicableExerciseTypes?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            IsActive: rubric.IsActive,
            Criteria: criteriaDtos);
    }
}
