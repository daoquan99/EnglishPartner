using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Prompts;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Prompts.Queries.ListCefrLevelPrompts;

public sealed record ListCefrLevelPromptsQuery() : IQuery<List<CefrLevelPromptEntity>>;

public sealed class ListCefrLevelPromptsQueryHandler : IQueryHandler<ListCefrLevelPromptsQuery, List<CefrLevelPromptEntity>>
{
    private readonly IAppDbContextFactory _dbFactory;

    public ListCefrLevelPromptsQueryHandler(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<CefrLevelPromptEntity>> HandleAsync(ListCefrLevelPromptsQuery query, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.CefrLevelPrompts
            .AsNoTracking()
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(ct);
    }
}
