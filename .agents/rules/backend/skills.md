---
trigger: always_on
---

# EnglishPartner — Core Skills

> **Scope:** Reusable templates + shared service usage guide (module-agnostic).
> Module-specific → `modules/{module}.md` | Infrastructure patterns → `common/{topic}.md`

---

## Skill 1: Shared Services — When & How to Use

### `ICurrentUserService`
**When:** Any handler that needs to know WHO is making the request.
**Rule:** Inject into **handler**, NEVER pass UserId via Command/Query record.
```csharp
internal sealed class MyCommandHandler(ICurrentUserService currentUser) : ICommandHandler<MyCommand>
{
    public async Task<Result> Handle(MyCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;   // guaranteed non-null (group RequireAuthorization)
        var email  = currentUser.Email;
        var isAdmin = currentUser.IsAdmin;
    }
}
```

### `ICacheService`
**When:** Read-heavy queries that can tolerate short staleness (vocabulary lists, leaderboard, etc.).
**Rule:** Use `GetOrSetAsync` (cache-aside). Key format: `{module}:{entity}:{discriminator}`.
```csharp
// Get or populate cache
var result = await cache.GetOrSetAsync(
    key: $"curriculum:vocab:{lang}:{page}:{pageSize}",
    factory: () => repo.GetPagedAsync(lang, page, pageSize, ct),
    expiry: TimeSpan.FromMinutes(30),
    cancellationToken: ct);

// Invalidate on write
await cache.RemoveByPrefixAsync("curriculum:vocab:", ct);
```

### `IRepository<T>`
**When:** All repository implementations extend this base.
**Methods:** `GetByIdAsync`, `GetByIdIncludingDeletedAsync`, `Add`, `Update`, `Remove` (soft-delete).
```csharp
// Soft-delete (audit)
repository.Remove(entity, currentUser.UserId, timeProvider.GetUtcNow().UtcDateTime);

// Hard-delete is forbidden on AggregateRoots — use Remove() which sets IsDeleted=true
```

### `IUnitOfWork`
**When:** NEVER call directly in handlers. The `UoWBehavior` MediatR pipeline behavior calls `SaveChangesAsync` automatically after every Command.
**Exception:** Only inject if you need explicit transaction control (rare — document why).

### `IFileStorageService`
**When:** Upload/download binary files (avatars, audio, documents).
```csharp
// Upload
var url = await fileStorage.UploadAsync(stream, fileName, folder: "avatars", ct);

// Get public URL from relative path
var publicUrl = fileStorage.GetPublicUrl(relativePath);

// Delete
await fileStorage.DeleteAsync(relativePath, ct);
```

### `TimeProvider`
**When:** ALWAYS — replace every occurrence of `DateTime.UtcNow` / `DateTimeOffset.UtcNow`.
```csharp
var utcNow = timeProvider.GetUtcNow().UtcDateTime;
```

---

## Skill 2: Command Template

```csharp
// Command record — only business data, NO UserId (handler gets from ICurrentUserService)
public sealed record CreateFooCommand(string Name, string? Description) : ICommand<Guid>;

// Validator
internal sealed class CreateFooCommandValidator : AbstractValidator<CreateFooCommand>
{
    public CreateFooCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

// Handler
internal sealed class CreateFooCommandHandler(
    IFooRepository repository,
    ICurrentUserService currentUser,
    TimeProvider timeProvider) : ICommandHandler<CreateFooCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateFooCommand request, CancellationToken ct)
    {
        var entity = Foo.Create(request.Name, request.Description);
        repository.Add(entity);
        return entity.Id;   // SaveChanges handled by UoWBehavior
    }
}
```

---

## Skill 3: Query Template (with optional cache)

```csharp
public sealed record GetFooListQuery(string Lang, int Page, int PageSize) : IQuery<List<FooDto>>;

public sealed record FooDto(Guid Id, string Name);

internal sealed class GetFooListQueryHandler(
    IFooRepository repository,
    ICacheService cache) : IQueryHandler<GetFooListQuery, List<FooDto>>
{
    public async Task<Result<List<FooDto>>> Handle(GetFooListQuery request, CancellationToken ct)
    {
        var key = $"module:foo:{request.Lang}:{request.Page}:{request.PageSize}";
        return await cache.GetOrSetAsync(key, async () =>
        {
            var items = await repository.GetPagedAsync(request.Lang, request.Page, request.PageSize, ct);
            return items.Select(x => new FooDto(x.Id, x.Name)).ToList();
        }, expiry: TimeSpan.FromMinutes(30), ct);
    }
}
```

---

## Skill 4: Domain Entity Template

```csharp
// AggregateRoot
public sealed class Foo : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;

    private Foo() { }  // EF Core

    public static Foo Create(string name)
    {
        var foo = new Foo { Id = Guid.NewGuid(), Name = name };
        foo.RaiseDomainEvent(new FooCreatedDomainEvent(foo.Id));
        return foo;
    }
}

// Domain Event
public sealed record FooCreatedDomainEvent(Guid FooId) : DomainEvent;

// Error class
public static class FooErrors
{
    public static readonly Error NotFound = Error.NotFound("Foo.NotFound", "Foo was not found.");
    public static readonly Error AlreadyExists = Error.Conflict("Foo.AlreadyExists", "Foo already exists.");
}
```

---

## Skill 5: Soft-Delete Pattern

```csharp
// Handler — soft delete with audit
internal sealed class DeleteFooCommandHandler(
    IFooRepository repository,
    ICurrentUserService currentUser,
    TimeProvider timeProvider) : ICommandHandler<DeleteFooCommand>
{
    public async Task<Result> Handle(DeleteFooCommand request, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(request.Id, ct);
        if (entity is null) return Result.Failure(FooErrors.NotFound);

        repository.Remove(entity, currentUser.UserId, timeProvider.GetUtcNow().UtcDateTime);
        return Result.Success();
    }
}
```

---

## Skill 6: Minimal API Endpoint Template

```csharp
internal static class FooEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/foo")
            .WithTags("Foo")
            .RequireAuthorization();

        group.MapPost("/", async (CreateFooRequest request, ISender sender) =>
        {
            var command = new CreateFooCommand(request.Name, request.Description);
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/v1/foo/{result.Value}", new { Id = result.Value })
                : result.ToProblemDetails();
        })
        .WithName("CreateFoo")
        .WithSummary("Create a new Foo.");
    }
}

public sealed record CreateFooRequest(string Name, string? Description = null);
```

---

## Skill 7: Module DI Installer Template

```csharp
// Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddFooInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<FooDbContext>((sp, options) =>
            options.UseNpgsql(config.GetConnectionString("Database"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "foo")));

        services.AddScoped<IFooUnitOfWork>(sp => sp.GetRequiredService<FooDbContext>());
        services.AddScoped<IFooRepository, FooRepository>();
        return services;
    }
}

// Api/ModuleInstaller.cs
public sealed class FooModule : IModuleInstaller
{
    public void Install(IServiceCollection services, IConfiguration config)
        => services.AddFooInfrastructure(config);

    public void Map(IEndpointRouteBuilder app)
        => FooEndpoints.Map(app);
}
```

---

## Skill 8: Code Formatting Standards

### Method Signatures — One Parameter Per Line
When a method has **3+ parameters**, break each parameter onto its own line:
```csharp
// ❌ Bad — long single line
public async Task<List<Foo>> GetAsync(string lang, int page, int pageSize, Guid? topicId, CancellationToken ct)

// ✅ Good — one per line
public async Task<List<Foo>> GetAsync(
    string lang,
    int page,
    int pageSize,
    Guid? topicId,
    CancellationToken cancellationToken = default)
```

### Call Sites — Named Arguments
When calling a method with **3+ arguments**, use **named arguments** for clarity:
```csharp
// ❌ Bad — positional arguments, hard to read
var results = await service.GenerateAsync("en", "Greetings", "A1", null, 5, null, 1, ct);

// ✅ Good — named arguments
var results = await service.GenerateAsync(
    targetLanguage: "en",
    context: "Greetings",
    level: "A1",
    scenario: null,
    lineCount: 5,
    targetLanguageCodes: null,
    count: 1,
    cancellationToken: ct);
```

### Object Creation — Named Arguments
Same rule for factory methods and constructors:
```csharp
// ❌ Bad
AiRequestLog.Create("eval", prompt, response, "gemini-2.0", 123, true, null, 10, 50);

// ✅ Good
AiRequestLog.Create(
    requestType: "eval",
    inputText: prompt,
    outputText: response,
    modelName: "gemini-2.0",
    latencyMs: 123,
    isSuccess: true,
    errorMessage: null,
    inputTokens: 10,
    outputTokens: 50);
```

> **Rule of thumb:** 1-2 args → inline is fine. 3+ args → break lines + name them.