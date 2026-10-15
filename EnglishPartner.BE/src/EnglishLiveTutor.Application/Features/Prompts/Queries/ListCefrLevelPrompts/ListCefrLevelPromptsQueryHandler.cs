using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Prompts;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Prompts.Queries.ListCefrLevelPrompts;

public sealed class ListCefrLevelPromptsQueryHandler : IQueryHandler<ListCefrLevelPromptsQuery, PagedResultDto<CefrLevelPromptEntity>>
{
    private readonly IAppDbContextFactory _dbFactory;

    public ListCefrLevelPromptsQueryHandler(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<PagedResultDto<CefrLevelPromptEntity>> HandleAsync(ListCefrLevelPromptsQuery query, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var q = db.CefrLevelPrompts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim().ToLowerInvariant();
            q = q.Where(c =>
                c.CefrLevel.ToLower().Contains(term) ||
                c.DisplayName.ToLower().Contains(term) ||
                c.LevelGuidance.ToLower().Contains(term));
        }

        var ordered = q.OrderBy(p => p.DisplayOrder);
        return await PagedResultDto<CefrLevelPromptEntity>.CreateAsync(ordered, query.Page, query.PageSize, ct);
    }
}
