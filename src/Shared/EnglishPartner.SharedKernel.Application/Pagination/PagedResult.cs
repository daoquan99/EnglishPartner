namespace EnglishPartner.SharedKernel.Pagination;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    public static PagedResult<T> Empty(int page = 1, int pageSize = 20)
        => new([], 0, page, pageSize);
}

public sealed class PaginationParams
{
    public PaginationParams(int page = 1, int pageSize = 20)
    {
        Page     = Math.Max(1, page);
        PageSize = Math.Clamp(pageSize, 1, 100);
    }

    public int Page     { get; }
    public int PageSize { get; }
    public int Skip     => (Page - 1) * PageSize;
    public int Take     => PageSize;
}

