using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Pronunciation.Rubrics;
using EnglishLiveTutor.Application.Features.Pronunciation.Rubrics.Models;
using EnglishLiveTutor.Application.Features.Pronunciation.Rubrics.Services;
using EnglishLiveTutor.Application.Modules.Speech;
using EnglishLiveTutor.Domain.Common.Exercises;
using EnglishLiveTutor.Domain.Pronunciation;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Rubrics.Queries;

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
    private readonly ISpeechDbContext _db;
    private readonly ILessonRubricDetailBuilder _rubricDetailBuilder;

    public RubricQueryHandlers(ISpeechDbContext db, ILessonRubricDetailBuilder rubricDetailBuilder)
    {
        _db = db;
        _rubricDetailBuilder = rubricDetailBuilder;
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

        var ordered = dbQuery.OrderBy(r => r.Code).ThenByDescending(r => r.Version);

        return await PagedResultDto<ScoringRubricEntity>.CreateProjectedAsync(
            ordered,
            (pageRubrics, _) => Task.FromResult(pageRubrics.Select(r => new LessonRubricSummaryDto(
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
            )).ToList()),
            query.Page ?? 1, // Lesson lists keep their historical default page (20 rows) when the client sends no paging.
            query.PageSize,
            ct);
    }

    public async Task<LessonRubricDetailDto?> HandleAsync(GetLessonRubricByIdQuery query, CancellationToken ct = default)
    {
        return await _rubricDetailBuilder.BuildDetailAsync(query.RubricId, ct);
    }
}
