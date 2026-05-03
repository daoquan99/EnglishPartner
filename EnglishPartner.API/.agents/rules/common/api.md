---
trigger: manual
---

# Common: API & Endpoint Patterns

> **Scope:** Minimal API endpoints, route conventions, ProblemDetails, authorization, rate limiting, SignalR
> @mention this file when working on: creating new endpoints, error responses, rate limit config, SignalR hubs

---

## 1. Endpoint Structure

```csharp
// File: Api/Endpoints/VocabularyEndpoints.cs
internal static class VocabularyEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // User group — authenticated, read-only
        var group = app.MapGroup("api/v1/curriculum/vocabularies")
            .WithTags("Vocabularies")
            .RequireAuthorization();

        // Admin group — push auth to group level, NOT per-endpoint
        var adminGroup = app.MapGroup("api/v1/admin/curriculum/vocabularies")
            .WithTags("Admin: Vocabularies")
            .RequireAuthorization("curriculum.vocabulary.write"); // ← group level

        group.MapGet("", async (...) => { ... }).WithName("GetVocabularies");
        adminGroup.MapPost("", async (...) => { ... }).WithName("CreateVocabulary");
        // ... không .RequireAuthorization() lại ở từng endpoint nếu group đã có
    }
}
```

**Thin endpoints:** chỉ xử lý HTTP concerns. Tất cả business logic qua `ISender` (MediatR).

---

## 2. Route Conventions

| Type | Pattern | Example |
|---|---|---|
| User list | `GET api/v1/{module}/{resources}` | `GET api/v1/curriculum/vocabularies` |
| User detail | `GET api/v1/{module}/{resources}/{id:guid}` | `GET api/v1/curriculum/vocabularies/{id}` |
| Admin CRUD | `api/v1/admin/{module}/{resources}` | `POST api/v1/admin/curriculum/vocabularies` |
| AI Generate | `POST api/v1/admin/{module}/generate/{resource}` | `POST api/v1/admin/curriculum/generate/vocabulary` |
| Auth | `api/v1/auth/{action}` | `POST api/v1/auth/login` |

---

## 3. Standard CRUD Endpoint Set

```
GET    /                     → list (paginated, filtered)
GET    /{id:guid}            → detail (single item)
POST   /                     → create → 201 Created
PUT    /{id:guid}            → update → 204 NoContent
DELETE /{id:guid}            → soft-delete → 204 NoContent
PUT    /{id:guid}/restore    → restore soft-deleted → 204 NoContent (admin)
PUT    /{id:guid}/activate   → activate → 204 NoContent (admin)
PUT    /{id:guid}/deactivate → deactivate → 204 NoContent (admin)
```

---

## 4. Result → HTTP Response Mapping

```csharp
// Pattern chuẩn cho mọi endpoint:
var result = await sender.Send(command);
return result.IsSuccess
    ? Results.Ok(result.Value)         // 200 with body
    : result.ToProblemDetails();       // ProblemDetails với error code

// Create → 201:
return result.IsSuccess
    ? Results.Created($"/api/v1/.../vocabularies/{result.Value}", result.Value)
    : result.ToProblemDetails();

// No content:
return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
```

### ProblemDetails Response Format:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Vocabulary with id 'abc' was not found.",
  "errors": [
    { "code": "Curriculum.Vocabulary.NotFound", "message": "..." }
  ]
}
```

> **Frontend:** đọc `errors[0].code` để distinguish errors — KHÔNG chỉ dựa vào HTTP status.

---

## 5. Authorization Strategy

### Group-Level (preferred):
```csharp
// ĐÚNG — push RequireAuthorization to group:
var adminGroup = app.MapGroup("api/v1/admin/curriculum/vocabularies")
    .RequireAuthorization("curriculum.vocabulary.write");

adminGroup.MapPost("", handler);     // ← kế thừa từ group, không cần thêm
adminGroup.MapPut("{id}", handler);  // ← kế thừa
```

### Không double-check:
```csharp
// SAI — redundant:
var adminGroup = app.MapGroup(...).RequireAuthorization("Admin");
adminGroup.MapPost("", handler).RequireAuthorization("curriculum.vocabulary.write"); // ← thừa
```

### Permission strings:
| Permission | Scope |
|---|---|
| `curriculum.vocabulary.write` | Create/Update/Delete vocabulary |
| `curriculum.generate` | AI generate endpoints |
| `learning.progress.write` | Update user progress |
| (tham khảo IdentityDbContext seeder để có đầy đủ) | |

---

## 6. Rate Limiting

### Configured policies (RateLimitingSettings):

| Policy key | Config path | Default | Used on |
|---|---|---|---|
| `ai-chat` | `RateLimiting:AIChat` | 10 req/60s | AI chat endpoints |
| `ai-scoring` | `RateLimiting:AIScoring` | 50 req/3600s | Exercise scoring |
| `ai-generate` | `RateLimiting:AIGenerate` | 10 req/60s | Curriculum generate endpoints |

### Applying to endpoints:
```csharp
endpoint.MapPost("/generate/vocabulary", handler)
    .RequireAuthorization("curriculum.generate")
    .RequireRateLimiting("ai-generate"); // ← bắt buộc cho AI endpoints
```

### Config in appsettings.json:
```json
"RateLimiting": {
  "AIChat":     { "PermitLimit": 10, "WindowSeconds": 60 },
  "AIScoring":  { "PermitLimit": 50, "WindowSeconds": 3600 },
  "AIGenerate": { "PermitLimit": 10, "WindowSeconds": 60 }
}
```

### 429 Response: `Retry-After` header auto-set bởi ASP.NET Core Rate Limiting middleware.

---

## 7. ID Mismatch Guard

```csharp
// Bắt buộc khi PUT endpoint nhận id từ route VÀ body:
if (id != command.Id)
    return Results.BadRequest("Route ID does not match body ID.");
```

---

## 8. SignalR Hub Pattern

| Hub | Auth method | Path |
|---|---|---|
| AI Live Hub | JWT via `?access_token=` query string | `/hubs/ai-live` |
| Notification Hub | Standard Bearer header | `/hubs/notifications` |

```csharp
// Hub registration trong ModuleInstaller / Program.cs:
app.MapHub<NotificationHub>("/hubs/notifications");
```
