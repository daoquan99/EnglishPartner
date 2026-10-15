using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Common.Models;

public sealed record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
)
{
    public static PagedResultDto<T> FromPage(
        IReadOnlyList<T> items,
        int totalCount,
        int? page = null,
        int? pageSize = null)
    {
        bool hasPaging = page.HasValue || pageSize.HasValue;
        int effPage = Math.Max(1, page ?? 1);
        int effSize = hasPaging
            ? Math.Clamp(pageSize ?? 20, 1, 500)
            : Math.Max(1, totalCount);
        int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)effSize));
        return new PagedResultDto<T>(items, totalCount, effPage, effSize, totalPages);
    }

    public static async Task<PagedResultDto<T>> CreateAsync(
        IQueryable<T> query,
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

        int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)effSize));
        return new PagedResultDto<T>(items, totalCount, effPage, effSize, totalPages);
    }

    public static async Task<PagedResultDto<TResult>> CreateProjectedAsync<TSource, TResult>(
        IQueryable<TSource> query,
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
        int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)effSize));
        return new PagedResultDto<TResult>(projected, totalCount, effPage, effSize, totalPages);
    }
}
