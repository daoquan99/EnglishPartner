using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Grammar.Common;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Grammar.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Queries.Admin;

public sealed record ListGrammarStepsAdminQuery(
    string? Level = null,
    int? Page = null,
    int? PageSize = null,
    bool? Paged = null
) : IQuery<PagedResultDto<GrammarStepAdminDto>>;

public sealed class ListGrammarStepsAdminQueryHandler : IQueryHandler<ListGrammarStepsAdminQuery, PagedResultDto<GrammarStepAdminDto>>
{
    private readonly IAppDbContext _db;

    public ListGrammarStepsAdminQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResultDto<GrammarStepAdminDto>> HandleAsync(ListGrammarStepsAdminQuery query, CancellationToken ct = default)
    {
        IQueryable<GrammarStepEntity> stepsQuery = _db.GrammarSteps
            .AsNoTracking()
            .Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Level))
        {
            string normLevel = query.Level.Trim().ToUpperInvariant();
            stepsQuery = stepsQuery.Where(s => s.CefrLevel == normLevel);
        }

        // Fetch all steps matching filter without TheoryJson, order in memory by CEFR level order then Order
        var allMatchingSteps = await stepsQuery
            .Select(s => new
            {
                s.Id,
                s.Code,
                s.CefrLevel,
                s.Order,
                s.TitleVi,
                s.TitleEn,
                s.SummaryVi,
                s.IsLevelReview,
                s.IsPublished,
                s.TheoryEditedByAdmin,
                s.TheoryVersion,
                s.CreatedAtUtc,
                s.UpdatedAtUtc
            })
            .ToListAsync(ct);
        var sortedSteps = allMatchingSteps
            .OrderBy(s => Array.IndexOf(GrammarRoadmapCalculator.LevelOrder, s.CefrLevel.ToUpperInvariant()))
            .ThenBy(s => s.Order)
            .ToList();

        int totalCount = sortedSteps.Count;
        bool hasPaging = query.Page.HasValue || query.PageSize.HasValue;
        int effPage = Math.Max(1, query.Page ?? 1);
        int effSize = hasPaging
            ? Math.Clamp(query.PageSize ?? PagedResultDto<GrammarStepAdminDto>.DefaultPageSize, 1, PagedResultDto<GrammarStepAdminDto>.MaxPageSize)
            : Math.Clamp(totalCount, 1, PagedResultDto<GrammarStepAdminDto>.MaxUnpagedItems);

        var pagedSteps = hasPaging
            ? sortedSteps.Skip((effPage - 1) * effSize).Take(effSize).ToList()
            : sortedSteps.Take(PagedResultDto<GrammarStepAdminDto>.MaxUnpagedItems).ToList();

        var stepIds = pagedSteps.Select(s => s.Id).ToList();

        // 2nd query: Aggregate counts for the steps on the current page
        var countsByStep = await _db.GrammarExercises
            .AsNoTracking()
            .Where(e => stepIds.Contains(e.StepId) && !e.IsDeleted)
            .GroupBy(e => e.StepId)
            .Select(g => new
            {
                StepId = g.Key,
                CoreCount = g.Count(e => e.Source == GrammarExerciseSource.Official && e.IsCore),
                OfficialCount = g.Count(e => e.Source == GrammarExerciseSource.Official),
                PendingCount = g.Count(e => e.Moderation == GrammarModerationStatus.Pending)
            })
            .ToDictionaryAsync(x => x.StepId, ct);

        var items = pagedSteps.Select(s =>
        {
            countsByStep.TryGetValue(s.Id, out var counts);
            return new GrammarStepAdminDto(
                Id: s.Id,
                Code: s.Code,
                CefrLevel: s.CefrLevel,
                Order: s.Order,
                TitleVi: s.TitleVi,
                TitleEn: s.TitleEn,
                SummaryVi: s.SummaryVi,
                IsLevelReview: s.IsLevelReview,
                IsPublished: s.IsPublished,
                TheoryEditedByAdmin: s.TheoryEditedByAdmin,
                TheoryVersion: s.TheoryVersion,
                CoreExerciseCount: counts?.CoreCount ?? 0,
                OfficialExerciseCount: counts?.OfficialCount ?? 0,
                PendingExerciseCount: counts?.PendingCount ?? 0,
                CreatedAtUtc: s.CreatedAtUtc,
                UpdatedAtUtc: s.UpdatedAtUtc
            );
        }).ToList();

        return PagedResultDto<GrammarStepAdminDto>.FromPage(items, totalCount, query.Page, query.PageSize);
    }
}
