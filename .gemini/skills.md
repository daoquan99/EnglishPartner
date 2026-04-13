# EnglishPartner - Development Skills

## Skill 1: Creating a New Module

When creating a new module, follow this exact structure:

### Step 1: Create Projects
Create 5 class library projects for the module:
```
src/Modules/{ModuleName}/
├── EnglishPartner.Modules.{ModuleName}.Api/
├── EnglishPartner.Modules.{ModuleName}.Application/
├── EnglishPartner.Modules.{ModuleName}.Domain/
├── EnglishPartner.Modules.{ModuleName}.Infrastructure/
└── EnglishPartner.Modules.{ModuleName}.Contracts/
```

### Step 2: Set Up Project References
```
Api → Application
Application → Domain
Infrastructure → Application, Domain
Contracts → (no module references, only SharedKernel if needed)
Api Host → Api, Infrastructure (for DI registration)
```

### Step 3: Create Module Installer
```csharp
// In Module.Api/ModuleInstaller.cs
public static class {ModuleName}ModuleInstaller
{
    public static IServiceCollection Add{ModuleName}Module(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Register Application services (MediatR, Validators)
        // Register Infrastructure services (DbContext, Repositories)
        // Register Module-specific services
        return services;
    }

    public static IApplicationBuilder Use{ModuleName}Module(
        this IApplicationBuilder app)
    {
        // Map endpoints
        return app;
    }
}
```

### Step 4: Register in Host
```csharp
// In EnglishPartner.Api/Program.cs
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddAIModule(builder.Configuration);
builder.Services.AddCurriculumModule(builder.Configuration);
builder.Services.AddLearningModule(builder.Configuration);
builder.Services.AddGamificationModule(builder.Configuration);
builder.Services.AddNotificationModule(builder.Configuration);

app.UseIdentityModule();
app.UseAIModule();
app.UseCurriculumModule();
app.UseLearningModule();
app.UseGamificationModule();
app.UseNotificationModule();
```

---

## Skill 2: Creating a Command (Write Operation)

### Step 1: Define the Command
```csharp
// Application/{Feature}/Commands/Create{Entity}/Create{Entity}Command.cs
public sealed record Create{Entity}Command(
    string Property1,
    string Property2
) : IRequest<Result<Guid>>;
```

### Step 2: Create the Validator
```csharp
// Application/{Feature}/Commands/Create{Entity}/Create{Entity}CommandValidator.cs
internal sealed class Create{Entity}CommandValidator
    : AbstractValidator<Create{Entity}Command>
{
    public Create{Entity}CommandValidator()
    {
        RuleFor(x => x.Property1)
            .NotEmpty()
            .MaximumLength(200);
    }
}
```

### Step 3: Implement the Handler
```csharp
internal sealed class Create{Entity}CommandHandler(
    I{Entity}Repository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<Create{Entity}Command, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        Create{Entity}Command request,
        CancellationToken cancellationToken)
    {
        var entity = {Entity}.Create(request.Property1, request.Property2);

        repository.Add(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(entity.Id);
    }
}
```

### Step 4: Create the Endpoint
```csharp
// Api/Endpoints/Admin/{Entity}Endpoints.cs
internal static class {Entity}AdminEndpoints
{
    internal static void Map{Entity}AdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/admin/{module}/{entities}")
            .RequireAuthorization("AdminPolicy")
            .WithTags("{Entity} Admin");

        group.MapPost("/", Create{Entity})
            .WithName("Create{Entity}")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> Create{Entity}(
        Create{Entity}Request request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new Create{Entity}Command(request.Property1, request.Property2);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/admin/{module}/{entities}/{result.Value}", result.Value)
            : result.ToProblemDetails();
    }
}
```

---

## Skill 3: Creating a Query (Read Operation)

### Step 1: Define the Query and Response DTO
```csharp
// Application/{Feature}/Queries/Get{Entity}ById/Get{Entity}ByIdQuery.cs
public sealed record Get{Entity}ByIdQuery(Guid Id) : IRequest<Result<{Entity}Response>>;

// Application/{Feature}/Queries/Get{Entity}ById/{Entity}Response.cs
public sealed record {Entity}Response(
    Guid Id,
    string Property1,
    string Property2,
    bool IsActive,
    DateTime CreatedAt,
    Guid? CreatedBy,
    DateTime? UpdatedAt,
    Guid? UpdatedBy
);
```

### Step 2: Implement the Handler
```csharp
// Application/{Feature}/Queries/Get{Entity}ById/Get{Entity}ByIdQueryHandler.cs
internal sealed class Get{Entity}ByIdQueryHandler(
    I{Entity}ReadRepository readRepository)
    : IRequestHandler<Get{Entity}ByIdQuery, Result<{Entity}Response>>
{
    public async Task<Result<{Entity}Response>> Handle(
        Get{Entity}ByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await readRepository.GetByIdAsync(request.Id, cancellationToken);

        if (entity is null)
            return Result.Failure<{Entity}Response>({Entity}Errors.NotFound(request.Id));

        return new {Entity}Response(
            entity.Id,
            entity.Property1,
            entity.Property2,
            entity.IsActive,
            entity.CreatedAt,
            entity.CreatedBy,
            entity.UpdatedAt,
            entity.UpdatedBy);
    }
}
```

---

## Skill 4: Creating Domain Entities (DDD)

### AggregateRoot (with audit + soft-delete)
```csharp
/// <summary>
/// Represents a {Entity} aggregate root in the bounded context.
/// </summary>
public sealed class {Entity} : AggregateRoot
{
    private {Entity}() { }

    public static {Entity} Create(string property1, string property2)
    {
        var entity = new {Entity}
        {
            Id = Guid.NewGuid(),
            Property1 = property1,
            Property2 = property2
        };

        entity.RaiseDomainEvent(new {Entity}CreatedDomainEvent(entity.Id));
        return entity;
    }

    public string Property1 { get; private set; } = default!;
    public string Property2 { get; private set; } = default!;

    public Result Update(string property1, string property2)
    {
        if (string.IsNullOrWhiteSpace(property1))
            return Result.Failure({Entity}Errors.InvalidProperty1);

        Property1 = property1;
        Property2 = property2;

        RaiseDomainEvent(new {Entity}UpdatedDomainEvent(Id));
        return Result.Success();
    }
}
```

### Child Entity (within an aggregate)
```csharp
public sealed class ConversationTurn : Entity
{
    private ConversationTurn() { }

    public static ConversationTurn Create(
        Guid conversationId, string speaker, string content, string contentVi, int order)
    {
        return new ConversationTurn
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Speaker = speaker,
            Content = content,
            ContentVi = contentVi,
            Order = order
        };
    }

    public Guid ConversationId { get; private set; }
    public string Speaker { get; private set; } = default!;
    public string Content { get; private set; } = default!;
    public string ContentVi { get; private set; } = default!;
    public int Order { get; private set; }
}
```

### Value Object
```csharp
// Domain/ValueObjects/Email.cs
public sealed record Email
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<Email>(IdentityErrors.EmailEmpty);

        if (!IsValidEmail(email))
            return Result.Failure<Email>(IdentityErrors.EmailInvalid);

        return new Email(email);
    }

    private static bool IsValidEmail(string email) =>
        Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
}
```

### Domain Event
```csharp
// Domain/Events/{Entity}CreatedDomainEvent.cs
public sealed record {Entity}CreatedDomainEvent(Guid {Entity}Id) : DomainEvent;
```

---

## Skill 5: Inter-Module Communication via Events

### Step 1: Define Integration Event (in Contracts)
```csharp
/// <summary>
/// Published when a new user registers.
/// Consumed by: Learning (create profile), Gamification (create profile), Notification (welcome).
/// </summary>
public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email,
    string DisplayName,
    DateTime RegisteredAt
) : IntegrationEvent;
```

### Step 2: Publish from Source Module
```csharp
internal sealed class UserRegisteredDomainEventHandler(
    IEventBus eventBus)
    : INotificationHandler<UserRegisteredDomainEvent>
{
    public async Task Handle(
        UserRegisteredDomainEvent notification,
        CancellationToken cancellationToken)
    {
        await eventBus.PublishAsync(
            new UserRegisteredIntegrationEvent(
                notification.UserId,
                notification.Email,
                notification.DisplayName,
                DateTime.UtcNow),
            cancellationToken);
    }
}
```

### Step 3: Handle in Target Module
```csharp
internal sealed class UserRegisteredIntegrationEventHandler(
    ILearnerProfileRepository repository,
    IUnitOfWork unitOfWork)
    : IIntegrationEventHandler<UserRegisteredIntegrationEvent>
{
    public async Task Handle(
        UserRegisteredIntegrationEvent @event,
        CancellationToken cancellationToken)
    {
        var profile = LearnerProfile.Create(@event.UserId, @event.DisplayName);
        repository.Add(profile);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

---

## Skill 6: Gemini AI Integration Pattern

### Service Interface (in AI.Domain)
```csharp
public interface IGeminiService
{
    Task<Result<T>> GenerateStructuredContentAsync<T>(
        string prompt,
        JsonSchema responseSchema,
        CancellationToken cancellationToken = default)
        where T : class;

    Task<Result<ChatResponse>> ChatAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> ChatStreamAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ScoringResult>> ScoreAnswerAsync(
        ScoringRequest request,
        CancellationToken cancellationToken = default);
}
```

### Implementation Pattern (in AI.Infrastructure)
```csharp
internal sealed class GeminiService : IGeminiService
{
    private readonly GenerativeModel _model;

    public GeminiService(IOptions<GeminiSettings> settings)
    {
        var client = new GenerativeAI(settings.Value.ApiKey);
        _model = client.CreateGenerativeModel(settings.Value.ModelName);
    }

    public async Task<Result<T>> GenerateStructuredContentAsync<T>(
        string prompt,
        JsonSchema responseSchema,
        CancellationToken cancellationToken) where T : class
    {
        var config = new GenerateContentConfig
        {
            ResponseMimeType = "application/json",
            ResponseSchema = responseSchema
        };

        var response = await _model.GenerateContentAsync(prompt, config);
        var result = JsonSerializer.Deserialize<T>(response.Text);

        return result is not null
            ? Result.Success(result)
            : Result.Failure<T>(AIErrors.DeserializationFailed);
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var streamResponse = _model.GenerateContentStreamAsync(request.Messages);
        await foreach (var chunk in streamResponse.WithCancellation(cancellationToken))
        {
            if (chunk.Text is not null)
                yield return chunk.Text;
        }
    }
}
```

---

## Skill 7: Adding Paginated Queries

```csharp
// SharedKernel - PagedResult
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

// Query with language filter
public sealed record GetVocabulariesQuery(
    string? SearchTerm,
    string? Level,
    string LanguageCode = "en",
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PagedResult<VocabularyResponse>>>;
```

---

## Skill 8: Configuring Module DbContext

```csharp
// Infrastructure/Database/{Module}DbContext.cs
internal sealed class CurriculumDbContext(
    DbContextOptions<CurriculumDbContext> options)
    : DbContext(options)
{
    internal DbSet<Vocabulary> Vocabularies => Set<Vocabulary>();
    internal DbSet<Grammar> Grammars => Set<Grammar>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("curriculum");
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CurriculumDbContext).Assembly);

        // Apply global query filter for soft-delete on all AggregateRoot entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AggregateRoot).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(
                        BuildSoftDeleteFilter(entityType.ClrType));
            }
        }
    }

    private static LambdaExpression BuildSoftDeleteFilter(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "e");
        var property = Expression.Property(parameter, nameof(AggregateRoot.IsDeleted));
        var condition = Expression.Equal(property, Expression.Constant(false));
        return Expression.Lambda(condition, parameter);
    }
}

// Register DbContext with AuditableEntityInterceptor
services.AddDbContext<CurriculumDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString)
        .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
});
```

---

## Skill 9: User vs Admin Endpoint Separation

### User Endpoints (Limited access, focused on learning)
```csharp
internal static class VocabularyUserEndpoints
{
    internal static void MapVocabularyUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/curriculum/vocabularies")
            .RequireAuthorization()
            .WithTags("Vocabulary");

        group.MapGet("/", GetVocabularies);        // Browse/search (filtered by language)
        group.MapGet("/{id:guid}", GetById);       // View detail
        group.MapGet("/daily", GetDailyWords);     // Daily vocabulary
    }
}
```

### Admin Endpoints (Full CRUD + management)
```csharp
internal static class VocabularyAdminEndpoints
{
    internal static void MapVocabularyAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/admin/curriculum/vocabularies")
            .RequireAuthorization("AdminPolicy")
            .WithTags("Vocabulary Admin");

        group.MapGet("/", GetAll);                          // List all with filters
        group.MapGet("/{id:guid}", GetById);                // View detail
        group.MapPost("/", Create);                         // Create
        group.MapPut("/{id:guid}", Update);                 // Update
        group.MapDelete("/{id:guid}", SoftDelete);          // Soft-delete (NOT hard-delete)
        group.MapPut("/{id:guid}/restore", Restore);        // Restore soft-deleted
        group.MapPut("/{id:guid}/activate", Activate);      // Activate
        group.MapPut("/{id:guid}/deactivate", Deactivate);  // Deactivate
        group.MapPost("/generate", GenerateWithAI);          // AI generation
    }
}
```

---

## Skill 10: Handling Streaks (Identity Module)

```csharp
// Domain Entity — child entity within User aggregate
public sealed class Streak : Entity
{
    public Guid UserId { get; private set; }
    public int CurrentStreak { get; private set; }
    public int LongestStreak { get; private set; }
    public DateOnly LastActivityDate { get; private set; }

    public Result CheckIn(DateOnly today)
    {
        if (LastActivityDate == today)
            return Result.Failure(StreakErrors.AlreadyCheckedIn);

        if (LastActivityDate == today.AddDays(-1))
        {
            CurrentStreak++;
        }
        else
        {
            CurrentStreak = 1; // Reset streak
        }

        if (CurrentStreak > LongestStreak)
            LongestStreak = CurrentStreak;

        LastActivityDate = today;

        RaiseDomainEvent(new StreakUpdatedDomainEvent(UserId, CurrentStreak));
        return Result.Success();
    }
}
```

---

## Skill 11: Scoring Pattern (Learning + AI Integration)

When a user completes an exercise:
1. **Learning Module** creates a `PracticeSession` and sends a scoring request.
2. **AI Module** receives the request, calls Gemini API to evaluate the answer.
3. **AI Module** publishes `AnswerScoredIntegrationEvent`.
4. **Learning Module** handles the event, records the score (keeping highest), and updates progress.
5. **Gamification Module** handles the event, awards XP based on score.

```
[User submits answer]
    → Learning.CreatePracticeSession
    → AI.ScoreAnswer (via Integration Event or direct call)
    → Gemini API evaluates
    → Learning.RecordScore (keep highest)
    → Learning.UpdateProgress (SM-2 spaced repetition)
    → Gamification.AwardXP
    → Notification.SendScoreNotification (optional)
```

---

## Skill 12: SharedKernel Base Classes

### Entity (with Audit Fields)
```csharp
public abstract class Entity : IEquatable<Entity>
{
    public Guid Id { get; protected set; }

    // ── Audit Fields (auto-populated by AuditableEntityInterceptor) ──
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // ── Equality ──
    public override bool Equals(object? obj)
        => obj is Entity entity && Id == entity.Id;

    public bool Equals(Entity? other) => other is not null && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right)
        => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right)
        => !Equals(left, right);
}
```

### AggregateRoot (with Soft-Delete + Active + Domain Events)
```csharp
public abstract class AggregateRoot : Entity
{
    // ── Lifecycle ──
    public bool IsActive { get; protected set; } = true;
    public bool IsDeleted { get; protected set; }
    public DateTime? DeletedAt { get; protected set; }
    public Guid? DeletedBy { get; protected set; }

    // ── Domain Events ──
    private readonly List<DomainEvent> _domainEvents = [];
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(DomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    // ── Lifecycle Behaviors ──
    public virtual void Activate() { IsActive = true; }
    public virtual void Deactivate() { IsActive = false; }

    public virtual void SoftDelete(Guid? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;
    }

    public virtual void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        IsActive = true;
    }
}
```

### Result Pattern
```csharp
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error) { ... }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

public class Result<T> : Result
{
    public T? Value { get; }
    // ...
}

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}
```

### ICurrentUserService (for Audit Interceptor)
```csharp
// SharedKernel
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
}

// Infrastructure (in Api Host)
internal sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserService
{
    public Guid? UserId => httpContextAccessor.HttpContext?.User
        .FindFirst(ClaimTypes.NameIdentifier)?.Value is string id
            ? Guid.Parse(id) : null;
    // ...
}
```

---

## Skill 13: Caching with Redis

### Cache Service Abstraction
```csharp
// SharedKernel
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}
```

### Usage in Query Handlers
```csharp
internal sealed class GetVocabularyByIdQueryHandler(
    IVocabularyReadRepository repository,
    ICacheService cache)
    : IRequestHandler<GetVocabularyByIdQuery, Result<VocabularyResponse>>
{
    public async Task<Result<VocabularyResponse>> Handle(
        GetVocabularyByIdQuery request, CancellationToken ct)
    {
        var cacheKey = $"curriculum:vocabulary:{request.Id}";
        var cached = await cache.GetAsync<VocabularyResponse>(cacheKey, ct);
        if (cached is not null) return cached;

        var entity = await repository.GetByIdAsync(request.Id, ct);
        if (entity is null)
            return Result.Failure<VocabularyResponse>(VocabularyErrors.NotFound(request.Id));

        var response = entity.ToResponse();
        await cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(30), ct);
        return response;
    }
}
```

### Cache Invalidation via Domain Events
```csharp
internal sealed class VocabularyUpdatedDomainEventHandler(ICacheService cache)
    : INotificationHandler<VocabularyUpdatedDomainEvent>
{
    public async Task Handle(VocabularyUpdatedDomainEvent notification, CancellationToken ct)
    {
        await cache.RemoveAsync($"curriculum:vocabulary:{notification.VocabularyId}", ct);
        await cache.RemoveByPrefixAsync("curriculum:vocabularies:", ct); // Invalidate list caches
    }
}
```

---

## Skill 14: Spaced Repetition (SM-2 Algorithm)

```csharp
// Learning Module - Domain Entity
public sealed class VocabularyProgress : Entity
{
    public Guid UserId { get; private set; }
    public Guid VocabularyId { get; private set; }

    // SM-2 State
    public int Repetitions { get; private set; }
    public double EaseFactor { get; private set; } = 2.5;
    public int IntervalDays { get; private set; } = 1;
    public DateOnly NextReviewDate { get; private set; }
    public string MasteryLevel { get; private set; } = "new";

    // Stats
    public int ReviewCount { get; private set; }
    public int CorrectCount { get; private set; }

    /// <summary>
    /// Apply SM-2 algorithm after a review.
    /// </summary>
    /// <param name="quality">0-5 scale (0=blackout, 5=perfect)</param>
    public void Review(int quality)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quality, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quality, 5);

        ReviewCount++;
        if (quality >= 3) CorrectCount++;

        // Update Ease Factor
        EaseFactor = Math.Max(1.3,
            EaseFactor + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02)));

        // Calculate next interval
        if (quality < 3)
        {
            Repetitions = 0;
            IntervalDays = 1;
        }
        else if (Repetitions == 0)
        {
            IntervalDays = 1;
            Repetitions++;
        }
        else if (Repetitions == 1)
        {
            IntervalDays = 6;
            Repetitions++;
        }
        else
        {
            IntervalDays = (int)Math.Round(IntervalDays * EaseFactor);
            Repetitions++;
        }

        NextReviewDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(IntervalDays);
        UpdateMasteryLevel();
    }

    private void UpdateMasteryLevel()
    {
        MasteryLevel = IntervalDays switch
        {
            <= 1 => "new",
            <= 7 => "learning",
            <= 30 => "reviewing",
            _ => "mastered"
        };
    }
}
```

---

## Skill 15: File Storage Abstraction

```csharp
// SharedKernel
public interface IFileStorageService
{
    /// <summary>Returns the relative URL path to the stored file.</summary>
    Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string folder,
        CancellationToken ct = default);

    Task<Stream?> DownloadAsync(string relativePath, CancellationToken ct = default);
    Task<bool> DeleteAsync(string relativePath, CancellationToken ct = default);
    string GetPublicUrl(string relativePath);
}

// Infrastructure - Local implementation
internal sealed class LocalFileStorageService(
    IOptions<FileStorageSettings> settings)
    : IFileStorageService
{
    // Stores at: {DataRoot}/uploads/{folder}/{guid}_{filename}
    // Returns: /uploads/{folder}/{guid}_{filename}
    // Future: Swap to AzureBlobStorageService or S3StorageService
}
```

---

## Skill 16: Soft-Delete and Restore Operations

### Command for Soft-Delete
```csharp
// Application/{Feature}/Commands/Delete{Entity}/Delete{Entity}Command.cs
public sealed record Delete{Entity}Command(Guid Id) : IRequest<Result>;

internal sealed class Delete{Entity}CommandHandler(
    I{Entity}Repository repository,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<Delete{Entity}Command, Result>
{
    public async Task<Result> Handle(Delete{Entity}Command request, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(request.Id, ct);
        if (entity is null)
            return Result.Failure({Entity}Errors.NotFound(request.Id));

        entity.SoftDelete(currentUser.UserId);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
```

### Command for Restore
```csharp
public sealed record Restore{Entity}Command(Guid Id) : IRequest<Result>;

internal sealed class Restore{Entity}CommandHandler(
    I{Entity}Repository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<Restore{Entity}Command, Result>
{
    public async Task<Result> Handle(Restore{Entity}Command request, CancellationToken ct)
    {
        // Must bypass global query filter to find soft-deleted entities
        var entity = await repository.GetByIdIncludingDeletedAsync(request.Id, ct);
        if (entity is null)
            return Result.Failure({Entity}Errors.NotFound(request.Id));

        entity.Restore();
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
```

### Admin Endpoint includes Soft-Delete view
```csharp
// Admin can view soft-deleted items
group.MapGet("/deleted", GetDeleted);       // List soft-deleted items
group.MapPut("/{id:guid}/restore", Restore); // Restore
```

---

## Skill 17: Multi-Language Content (Future-Ready)

### Language entity
```csharp
// Curriculum module
public sealed class Language : AggregateRoot
{
    public string Code { get; private set; } = default!;     // "en", "zh", "ja", "ko"
    public string Name { get; private set; } = default!;     // "English", "Chinese"
    public string NativeName { get; private set; } = default!; // "English", "中文"
    public string FlagEmoji { get; private set; } = default!;  // "🇬🇧", "🇨🇳"
    public int Order { get; private set; }
}
```

### Content entities have LanguageCode
```csharp
public sealed class Vocabulary : AggregateRoot
{
    public string LanguageCode { get; private set; } = "en"; // Default English
    public Guid TopicId { get; private set; }
    public string Word { get; private set; } = default!;
    // ... other properties
}
```

### All content queries accept language filter
```csharp
public sealed record GetVocabulariesQuery(
    string LanguageCode = "en",  // Default to English
    string? SearchTerm = null,
    int? DifficultyLevel = null,
    Guid? TopicId = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PagedResult<VocabularyResponse>>>;
```
