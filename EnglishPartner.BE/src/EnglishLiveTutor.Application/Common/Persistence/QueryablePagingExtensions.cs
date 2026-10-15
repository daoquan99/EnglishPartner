using EnglishLiveTutor.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Common.Persistence;

public static class QueryablePagingExtensions
{
    public static async Task<PagedResultDto<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int? page = null,
        int? pageSize = null,
        CancellationToken ct = default)
    {
        int totalCount = await query.CountAsync(ct);
        bool hasPaging = page.HasValue || pageSize.HasValue;
        int effPage = Math.Max(1, page ?? 1);
        int effSize = hasPaging
            ? Math.Clamp(pageSize ?? 20, 1, 500)
            : Math.Max(1, totalCount);

        List<T> items = hasPaging
            ? await query.Skip((effPage - 1) * effSize).Take(effSize).ToListAsync(ct)
            : await query.ToListAsync(ct);

        return PagedResultDto<T>.FromPage(items, totalCount, page, pageSize);
    }

    public static async Task<PagedResultDto<TResult>> ToProjectedPagedResultAsync<TSource, TResult>(
        this IQueryable<TSource> query,
        Func<List<TSource>, CancellationToken, Task<List<TResult>>> pageProjectorAsync,
        int? page = null,
        int? pageSize = null,
        CancellationToken ct = default)
    {
        int totalCount = await query.CountAsync(ct);
        bool hasPaging = page.HasValue || pageSize.HasValue;
        int effPage = Math.Max(1, page ?? 1);
        int effSize = hasPaging
            ? Math.Clamp(pageSize ?? 20, 1, 500)
            : Math.Max(1, totalCount);

        List<TSource> pageSources = hasPaging
            ? await query.Skip((effPage - 1) * effSize).Take(effSize).ToListAsync(ct)
            : await query.ToListAsync(ct);

        List<TResult> projected = await pageProjectorAsync(pageSources, ct);
        return PagedResultDto<TResult>.FromPage(projected, totalCount, page, pageSize);
    }
}
