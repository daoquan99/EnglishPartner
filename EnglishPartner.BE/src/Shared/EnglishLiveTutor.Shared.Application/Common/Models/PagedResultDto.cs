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
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int MaxUnpagedItems = 1000;

    public static PagedResultDto<T> FromPage(
        IReadOnlyList<T> items,
        int totalCount,
        int? page = null,
        int? pageSize = null)
    {
        bool hasPaging = page.HasValue || pageSize.HasValue;
        int effPage = Math.Max(1, page ?? 1);
        int effSize = hasPaging
            ? Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize)
            : Math.Clamp(totalCount, 1, MaxUnpagedItems);

        IReadOnlyList<T> boundedItems = (!hasPaging && items.Count > MaxUnpagedItems)
            ? items.Take(MaxUnpagedItems).ToList()
            : items;

        int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)Math.Max(1, effSize)));
        return new PagedResultDto<T>(boundedItems, totalCount, effPage, effSize, totalPages);
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
            ? Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize)
            : Math.Clamp(totalCount, 1, MaxUnpagedItems);

        List<T> items = hasPaging
            ? await query.Skip((effPage - 1) * effSize).Take(effSize).ToListAsync(ct)
            : await query.Take(MaxUnpagedItems).ToListAsync(ct);

        int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)Math.Max(1, effSize)));
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
            ? Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize)
            : Math.Clamp(totalCount, 1, MaxUnpagedItems);

        List<TSource> pageSources = hasPaging
            ? await query.Skip((effPage - 1) * effSize).Take(effSize).ToListAsync(ct)
            : await query.Take(MaxUnpagedItems).ToListAsync(ct);

        List<TResult> projected = await pageProjectorAsync(pageSources, ct);
        int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)Math.Max(1, effSize)));
        return new PagedResultDto<TResult>(projected, totalCount, effPage, effSize, totalPages);
    }
}
