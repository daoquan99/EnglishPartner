using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Lessons.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Lessons.Queries.RubricQueries;

public sealed record ListLessonRubricsQuery(
    string? ExerciseType = null,
    string? CefrLevel = null,
    bool? OnlyActive = null,
    int? Page = null,
    int? PageSize = null
) : IQuery<PagedResultDto<LessonRubricSummaryDto>>;

public sealed record GetLessonRubricByIdQuery(Guid RubricId) : IQuery<LessonRubricDetailDto?>;

public sealed class RubricQueryHandlers :
    IQueryHandler<ListLessonRubricsQuery, PagedResultDto<LessonRubricSummaryDto>>,
    IQueryHandler<GetLessonRubricByIdQuery, LessonRubricDetailDto?>
{
    private readonly IAppDbContext _db;

    public RubricQueryHandlers(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResultDto<LessonRubricSummaryDto>> HandleAsync(ListLessonRubricsQuery query, CancellationToken ct = default)
    {
        var dbQuery = _db.ScoringRubrics
            .AsNoTracking()
            .Include(r => r.Criteria)
            .Where(r => r.AssessmentScope == "LessonExercise")
            .AsQueryable();

        if (query.OnlyActive.HasValue)
        {
            dbQuery = dbQuery.Where(r => r.IsActive == query.OnlyActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.CefrLevel))
        {
            string cefr = query.CefrLevel.Trim().ToUpperInvariant();
            dbQuery = dbQuery.Where(r => r.CefrLevel == "All" || r.CefrLevel == cefr);
        }

        if (!string.IsNullOrWhiteSpace(query.ExerciseType) && Enum.TryParse<ExerciseType>(query.ExerciseType, ignoreCase: true, out var exType))
        {
            int exTypeVal = (int)exType;
            string exTypeName = exType.ToString();
            dbQuery = dbQuery.Where(r => r.ExerciseType == exTypeVal || (r.ApplicableExerciseTypes != null && r.ApplicableExerciseTypes.Contains(exTypeName)));
        }

        dbQuery = dbQuery.OrderBy(r => r.Code).ThenByDescending(r => r.Version);

        int totalCount = await dbQuery.CountAsync(ct);
        int page = Math.Max(1, query.Page ?? 1);
        int pageSize = Math.Clamp(query.PageSize ?? 20, 1, 500);

        var rubrics = await dbQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = rubrics.Select(r => new LessonRubricSummaryDto(
            Id: r.Id,
            Code: r.Code,
            Name: r.Name,
            Description: r.Description,
            Version: r.Version,
            AssessmentScope: r.AssessmentScope,
            ExerciseType: r.ExerciseType.HasValue ? ((ExerciseType)r.ExerciseType.Value).ToString() : null,
            SubQuestionKind: r.SubQuestionKind,
            CefrLevel: r.CefrLevel,
            ApplicableExerciseTypes: r.ApplicableExerciseTypes?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            IsActive: r.IsActive,
            CriteriaCount: r.Criteria.Count
        )).ToList();

        int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        return new PagedResultDto<LessonRubricSummaryDto>(dtos, totalCount, page, pageSize, totalPages);
    }

    public async Task<LessonRubricDetailDto?> HandleAsync(GetLessonRubricByIdQuery query, CancellationToken ct = default)
    {
        var rubric = await _db.ScoringRubrics
            .AsNoTracking()
            .Include(r => r.Criteria)
            .FirstOrDefaultAsync(r => r.Id == query.RubricId, ct);

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
