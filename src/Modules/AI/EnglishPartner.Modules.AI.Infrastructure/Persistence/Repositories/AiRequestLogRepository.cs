using EnglishPartner.Modules.AI.Domain.AiRequestLogs;
using EnglishPartner.Modules.AI.Domain.AiRequestLogs.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.AI.Infrastructure.Persistence.Repositories;

internal sealed class AiRequestLogRepository(AiDbContext context) : IAiRequestLogRepository
{
    public async Task<(IReadOnlyList<AiRequestLog> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? requestType = null,
        bool? isSuccess = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.AiRequestLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(requestType))
            query = query.Where(x => x.RequestType == requestType);

        if (isSuccess.HasValue)
            query = query.Where(x => x.IsSuccess == isSuccess.Value);

        if (from.HasValue)
            query = query.Where(x => x.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.CreatedAt <= to.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
