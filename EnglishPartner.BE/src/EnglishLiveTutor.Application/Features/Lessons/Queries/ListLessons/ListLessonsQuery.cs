using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Lessons.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Lessons.Queries.ListLessons;

public sealed record ListLessonsQuery(
    string? Search = null,
    string? CefrLevel = null,
    string? Topic = null,
    LessonStatus? Status = null,
    bool IsLearner = false,
    int? Page = null,
    int? PageSize = null
) : IQuery<PagedResultDto<LessonSummaryDto>>;

public sealed class ListLessonsQueryHandler : IQueryHandler<ListLessonsQuery, PagedResultDto<LessonSummaryDto>>
{
    private readonly IAppDbContext _db;

    public ListLessonsQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResultDto<LessonSummaryDto>> HandleAsync(ListLessonsQuery query, CancellationToken ct = default)
    {
        var dbQuery = _db.Lessons.AsNoTracking().Include(l => l.Exercises).AsQueryable();

        if (query.IsLearner)
        {
            dbQuery = dbQuery.Where(l => l.Status == LessonStatus.Published);
        }
        else if (query.Status.HasValue)
        {
            dbQuery = dbQuery.Where(l => l.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.CefrLevel))
        {
            string cefr = query.CefrLevel.Trim().ToUpperInvariant();
            dbQuery = dbQuery.Where(l => l.CefrLevel == cefr);
        }

        if (!string.IsNullOrWhiteSpace(query.Topic))
        {
            string topic = query.Topic.Trim();
            dbQuery = dbQuery.Where(l => l.Topic == topic);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim().ToLowerInvariant();
            dbQuery = dbQuery.Where(l => l.Title.ToLower().Contains(term) || l.Topic.ToLower().Contains(term));
        }

        dbQuery = dbQuery.OrderByDescending(l => l.CreatedAtUtc);

        int totalCount = await dbQuery.CountAsync(ct);
        int page = Math.Max(1, query.Page ?? 1);
        int pageSize = Math.Clamp(query.PageSize ?? 20, 1, 500);

        var lessons = await dbQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = lessons.Select(l => new LessonSummaryDto(
            Id: l.Id,
            Title: l.Title,
            CefrLevel: l.CefrLevel,
            Topic: l.Topic,
            Content: l.Content,
            EstimatedMinutes: l.EstimatedMinutes,
            Status: l.Status.ToString(),
            Version: l.Version,
            ExerciseCount: l.Exercises.Count,
            TotalMaxPoints: l.Exercises.Sum(e => e.MaxPoints),
            PublishedAtUtc: l.PublishedAtUtc,
            RowVersion: l.RowVersion != null ? Convert.ToBase64String(l.RowVersion) : null
        )).ToList();

        int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        return new PagedResultDto<LessonSummaryDto>(dtos, totalCount, page, pageSize, totalPages);
    }
}
