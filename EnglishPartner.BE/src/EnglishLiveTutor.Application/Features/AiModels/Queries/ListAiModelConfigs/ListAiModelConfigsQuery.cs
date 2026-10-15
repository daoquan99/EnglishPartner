using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.AiCatalog;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.AiModels.Queries.ListAiModelConfigs;

public sealed record ListAiModelConfigsQuery(string? Search = null, int? Page = null, int? PageSize = null) : IQuery<PagedResultDto<AiModelConfigEntity>>;

public sealed class ListAiModelConfigsQueryHandler(IAppDbContextFactory dbFactory) : IQueryHandler<ListAiModelConfigsQuery, PagedResultDto<AiModelConfigEntity>>
{
    public async Task<PagedResultDto<AiModelConfigEntity>> HandleAsync(ListAiModelConfigsQuery request, CancellationToken ct = default)
    {
        var search = request.Search;
        var page = request.Page;
        var pageSize = request.PageSize;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var query = db.AiModelCatalog.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLowerInvariant();
            query = query.Where(m =>
                m.DisplayName.ToLower().Contains(term) ||
                m.ModelCode.ToLower().Contains(term) ||
                (m.CapabilityNote != null && m.CapabilityNote.ToLower().Contains(term)));
        }

        var ordered = query
            .OrderBy(m => m.DisplayOrder)
            .ThenBy(m => m.DisplayName)
            .ThenBy(m => m.Id);

        return await PagedResultDto<AiModelConfigEntity>.CreateAsync(ordered, page, pageSize, ct);
    }
}
