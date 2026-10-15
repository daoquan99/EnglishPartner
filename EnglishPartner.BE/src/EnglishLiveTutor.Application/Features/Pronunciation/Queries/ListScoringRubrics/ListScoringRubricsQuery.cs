using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Pronunciation;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Queries.ListScoringRubrics;

public sealed record ListScoringRubricsQuery(string? Search = null, int? Page = null, int? PageSize = null) : IQuery<PagedResultDto<ScoringRubricEntity>>;

public sealed class ListScoringRubricsQueryHandler(IAppDbContextFactory dbFactory) : IQueryHandler<ListScoringRubricsQuery, PagedResultDto<ScoringRubricEntity>>
{
    public async Task<PagedResultDto<ScoringRubricEntity>> HandleAsync(ListScoringRubricsQuery request, CancellationToken ct = default)
    {
        var search = request.Search;
        var page = request.Page;
        var pageSize = request.PageSize;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var query = db.ScoringRubrics
            .AsNoTracking()
            .Include(r => r.Criteria.OrderBy(c => c.DisplayOrder))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLowerInvariant();
            query = query.Where(r =>
                r.Code.ToLower().Contains(term) ||
                r.Name.ToLower().Contains(term) ||
                r.Description.ToLower().Contains(term));
        }

        var ordered = query
            .OrderByDescending(r => r.IsActive)
            .ThenBy(r => r.Code)
            .ThenBy(r => r.Id);

        return await PagedResultDto<ScoringRubricEntity>.CreateAsync(ordered, page, pageSize, ct);
    }
}
