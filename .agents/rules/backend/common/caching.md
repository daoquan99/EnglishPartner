---
trigger: manual
---

# Common: Caching Patterns

> **Scope:** Redis cache strategy, ICacheService, key conventions, cache invalidation
> @mention this file when working on: adding cache to queries, cache invalidation, Redis keys

---

## 1. ICacheService Interface

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken ct = default);
}
```

Namespace: `EnglishPartner.SharedKernel.Interfaces`

---

## 2. Automatic Caching (ICacheableQuery)

**Preferred pattern** cho Queries: Sử dụng `ICacheableQuery<TResponse>` thay vì `IQuery<TResponse>`. 
`QueryCachingBehavior` của MediatR sẽ tự động kiểm tra cache trước khi chạy Handler, và lưu cache sau khi chạy xong.

```csharp
// 1. Kế thừa ICacheableQuery
public sealed record GetGrammarsQuery(string LanguageCode) : ICacheableQuery<List<GrammarResponse>>
{
    public string CacheKey => CacheKeys.Curriculum.GrammarsPrefix(LanguageCode);
    public TimeSpan? Expiration => CacheTtl.Medium;
}

// 2. Handler KHÔNG CẦN đụng đến ICacheService
internal sealed class GetGrammarsQueryHandler(IGrammarRepository repo) 
    : IQueryHandler<GetGrammarsQuery, List<GrammarResponse>>
{
    public async Task<Result<List<GrammarResponse>>> Handle(GetGrammarsQuery request, CancellationToken ct)
    {
        // Chạy thẳng logic query DB
        var items = await repo.GetAllAsync(request.LanguageCode, ct);
        return items;
    }
}
```

---

## 3. Manual Cache-Aside (GetOrSetAsync)

Dùng cho các trường hợp phức tạp hơn không thể dùng `ICacheableQuery` (ví dụ có phân trang hoặc search term):

```csharp
internal sealed class GetVocabulariesQueryHandler(
    IVocabularyRepository vocabularyRepository,
    ICacheService cache) : IQueryHandler<GetVocabulariesQuery, VocabulariesResponse>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    public async Task<Result<VocabulariesResponse>> Handle(GetVocabulariesQuery request, CancellationToken ct)
    {
        var cacheKey = CacheKeys.Curriculum.VocabulariesQuery(request.LanguageCode, request.Page, request.PageSize, request.Level, request.TopicId);

        var response = await cache.GetOrSetAsync(cacheKey, async () =>
        {
            var (items, total) = await vocabularyRepository.GetPagedAsync(...);
            return new VocabulariesResponse(/* map items */);
        }, CacheTtl.Medium, ct);

        return response!;
    }
}
```

**Skip cache khi có search term** — search results quá dynamic:
```csharp
if (!string.IsNullOrWhiteSpace(request.SearchTerm))
{
    // Truy vấn thẳng DB, không cache
    var result = await repo.SearchAsync(...);
    return Map(result);
}
// else: dùng GetOrSetAsync như trên
```

---

## 4. Centralized Cache Keys and TTLs

All cache keys and Default TTLs MUST be centralized to avoid scattering magic strings across the project.
Use `EnglishPartner.SharedKernel.Application.Caching.CacheKeys` to generate redis keys.
Use `EnglishPartner.SharedKernel.Application.Caching.CacheTtl` to provide standard Time-To-Live.

| Pattern | Format | Example |
|---|---|---|
| Single item | `CacheKeys.Module.Entity(id)` | `CacheKeys.Identity.Session(id)` |
| Paginated list (no filter) | `CacheKeys.Module.EntityQuery(...)` | `CacheKeys.Curriculum.VocabulariesQuery(...)` |
| Prefix convention | `CacheKeys.Module.EntityPrefix(...)` | `CacheKeys.Curriculum.VocabulariesPrefix(lang)` |

---

## 5. TTL Policy

| Data type | TTL | Lý do |
|---|---|---|
| Vocabulary / Grammar list | 30 min | Read-heavy, ít thay đổi |
| Topic list | 60 min | Rất ít thay đổi |
| Single item detail | 15 min | Có thể update |
| Redis session | 7 days (sliding) | = Refresh Token lifetime |
| Leaderboard | 5 min | Update thường xuyên |
| Admin dashboard stats | 60 sec | Cần gần real-time |
| AI audio (IMemoryCache) | 10 min | In-process only |

---

## 6. Cache Invalidation

### Inline trong Command Handler (hiện tại)
```csharp
// Trong Create/Update/Delete handlers:
await cache.RemoveByPrefixAsync(CacheKeys.Curriculum.VocabulariesPrefix(languageCode), ct);
```

### Via Domain Events (preferred — decoupled)
```csharp
// Domain Event raised trong entity:
entity.RaiseDomainEvent(new VocabularyUpdatedDomainEvent(entity.Id, entity.LanguageCode));

// Handler trong Application layer:
internal sealed class VocabularyUpdatedHandler(ICacheService cache)
    : INotificationHandler<VocabularyUpdatedDomainEvent>
{
    public async Task Handle(VocabularyUpdatedDomainEvent notification, CancellationToken ct)
    {
        await cache.RemoveByPrefixAsync(CacheKeys.Curriculum.VocabulariesPrefix(notification.LanguageCode), ct);
    }
}
```
> Domain Event approach giữ Command Handler không biết về cache — tốt hơn cho maintainability.

---

## 7. IMemoryCache (In-Process)

- **CHỈ dùng cho:** Gemini TTS audio responses (avoid repeated AI API calls)
- **KHÔNG dùng cho:** shared state, distributed data
- **Key:** `tts:{text_hash}:{voice}`
- Injection: `IMemoryCache` (standard ASP.NET Core)

---

## 8. AppSettings mapping

`CacheSettings` map section `Cache` từ `appsettings.json` được cấu hình dạng Global Singleton:
```csharp
CacheSettings.Initialize(builder.Configuration.GetSection("Cache").Get<CacheSettings>());
```
Để sử dụng, bạn có thể gọi trực tiếp `CacheSettings.Instance` thay vì phải dùng `IOptions<CacheSettings>` (giúp dễ dàng truy cập từ bất cứ đâu).

---

## 9. Quan trọng

- `ICacheService` → inject vào Application layer (Query handlers, Command handlers cần invalidation)
- Không inject vào Domain layer
- `RemoveByPrefixAsync` cần Redis SCAN — đảm bảo Redis implementation hỗ trợ
- Cache chỉ nên áp dụng cho data **ít thay đổi** và **đọc nhiều** — không cache write-heavy data
