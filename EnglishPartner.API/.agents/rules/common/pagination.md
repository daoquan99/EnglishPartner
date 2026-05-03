---
trigger: manual
---

# Common: Pagination

> **Scope:** PagedResult<T>, PaginationParams, paginated queries
> @mention this file when working on: list endpoints, paginated queries

---

## 1. Pagination Primitives

### `PaginationParams`
Use `PaginationParams` in Queries or directly as query record properties.
- Minimum page is 1.
- Maximum pageSize is 100.
- Use `Skip` and `Take` properties for EF Core queries.

### `PagedResult<T>`
Always return a `PagedResult<T>` when returning a list of items that could grow large.

```csharp
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
```

---

## 2. Implementing Paginated Queries

### 1. The Query Record
```csharp
public sealed record GetFoosQuery(int Page = 1, int PageSize = 20, string? SearchTerm = null) 
    : ICacheableQuery<PagedResult<FooResponse>>;
```

### 2. The Repository Interface
```csharp
Task<(List<Foo> Items, int TotalCount)> GetPagedAsync(
    int page, int pageSize, string? searchTerm, CancellationToken ct = default);
```

### 3. The Repository Implementation (EF Core)
```csharp
public async Task<(List<Foo> Items, int TotalCount)> GetPagedAsync(
    int page, int pageSize, string? searchTerm, CancellationToken ct = default)
{
    var query = dbContext.Foos.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        query = query.Where(x => EF.Functions.ILike(x.Name, $"%{searchTerm}%"));
    }

    var totalCount = await query.CountAsync(ct);

    var parameters = new PaginationParams(page, pageSize);
    var items = await query
        .OrderByDescending(x => x.CreatedAt)
        .Skip(parameters.Skip)
        .Take(parameters.Take)
        .ToListAsync(ct);

    return (items, totalCount);
}
```

### 4. The Handler
```csharp
internal sealed class GetFoosQueryHandler(IFooRepository repo) 
    : IQueryHandler<GetFoosQuery, PagedResult<FooResponse>>
{
    public async Task<Result<PagedResult<FooResponse>>> Handle(GetFoosQuery request, CancellationToken ct)
    {
        var (items, totalCount) = await repo.GetPagedAsync(request.Page, request.PageSize, request.SearchTerm, ct);
        
        var dtos = items.Select(x => new FooResponse(x.Id, x.Name)).ToList();
        
        return new PagedResult<FooResponse>(dtos, totalCount, request.Page, request.PageSize);
    }
}
```
