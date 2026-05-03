---
trigger: manual
---

# Common: Database Patterns

> **Scope:** DbContext setup, EF Core config, Migrations, Soft-delete, Global filters, Interceptors, Repository
> @mention this file when working on: creating new DbContext, entity configuration, migrations, soft-delete, restore, repository

---

## 1. DbContext Template

```csharp
public sealed class CurriculumDbContext(
    DbContextOptions<CurriculumDbContext> options,
    AuditableEntityInterceptor auditInterceptor,
    ConvertDomainEventsToOutboxMessagesInterceptor outboxInterceptor)
    : DbContext(options), ICurriculumUnitOfWork
{
    internal const string Schema = "curriculum"; // ← mỗi module có schema riêng

    public DbSet<Vocabulary> Vocabularies => Set<Vocabulary>();
    // ...

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.AddInterceptors(auditInterceptor, outboxInterceptor);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Ignore<DomainEvent>(); // ← bắt buộc
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CurriculumDbContext).Assembly);
        CurriculumDataSeeder.Seed(modelBuilder); // ← optional seeder
    }
}
```

**MigrationsHistoryTable:** Phải đặt trong module schema — config trong `DependencyInjection.cs`:
```csharp
options.UseNpgsql(conn, o => o.MigrationsHistoryTable("__EFMigrationsHistory", "curriculum"));
```

---

## 2. Entity Configuration (IEntityTypeConfiguration)

```csharp
internal sealed class VocabularyConfiguration : IEntityTypeConfiguration<Vocabulary>
{
    public void Configure(EntityTypeBuilder<Vocabulary> builder)
    {
        builder.ToTable("vocabularies", CurriculumDbContext.Schema);
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Word).HasMaxLength(200).IsRequired();
        builder.Property(v => v.LanguageCode).HasMaxLength(10).IsRequired();
        // PostgreSQL text array:
        builder.Property(v => v.Synonyms).HasColumnType("text[]");
        // Global query filter — auto-filter deleted:
        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}
```

**Owned entities** (ExerciseOption, ConversationLine):
```csharp
builder.OwnsMany(e => e.Options, ob => {
    ob.ToTable("exercise_options", Schema);
    ob.WithOwner().HasForeignKey("ExerciseId");
});
```

---

## 3. Global Query Filters

- **`IsDeleted == false`** — áp dụng cho mọi AggregateRoot trong `OnModelCreating`
- Tất cả queries **tự động** loại bỏ soft-deleted records
- **Bypass filter** (admin GetDeleted): `.IgnoreQueryFilters()`
- **KHÔNG** cần `.Where(x => !x.IsDeleted)` thủ công trong repository

---

## 4. Interceptors

### AuditableEntityInterceptor
- Auto-set `CreatedAt`, `CreatedBy` khi `EntityState.Added`
- Auto-set `UpdatedAt`, `UpdatedBy` khi `EntityState.Modified`
- Đọc `ICurrentUserService.UserId` từ DI
- Dùng `TimeProvider.GetUtcNow()` — NOT `DateTime.UtcNow`

### ConvertDomainEventsToOutboxMessagesInterceptor
- Sau mỗi `SaveChanges`, quét tất cả entities có DomainEvents
- Serialize mỗi event → `OutboxMessage` row trong cùng transaction
- Xóa events khỏi entity sau khi đã enqueue

---

## 5. Soft-Delete Pattern

### AggregateRoot.SoftDelete signature:
```csharp
public virtual void SoftDelete(Guid? deletedBy = null, DateTime? deletedAt = null)
{
    IsDeleted = true;
    DeletedAt = deletedAt ?? DateTime.UtcNow;  // prefer passing from TimeProvider
    DeletedBy = deletedBy;
    IsActive = false;
}
```

### Repository pattern — LUÔN truyền audit params:
```csharp
// Interface (Domain layer):
void Remove(TEntity entity, Guid? deletedBy = null, DateTime? deletedAt = null);

// Implementation (Infrastructure layer):
public void Remove(Vocabulary entity, Guid? deletedBy = null, DateTime? deletedAt = null)
    => entity.SoftDelete(deletedBy, deletedAt);
```

### Delete Command Handler:
```csharp
internal sealed class DeleteVocabularyCommandHandler(
    IVocabularyRepository repo,
    ICurrentUserService currentUser,
    TimeProvider timeProvider,
    ICacheService cache) : ICommandHandler<DeleteVocabularyCommand>
{
    public async Task<Result> Handle(DeleteVocabularyCommand request, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(request.Id, ct);
        if (entity is null) return Result.Failure(CurriculumErrors.Vocabulary.NotFound(request.Id));

        repo.Remove(entity, currentUser.UserId, timeProvider.GetUtcNow().UtcDateTime);
        await cache.RemoveByPrefixAsync($"curriculum:vocabularies:{entity.LanguageCode}:", ct);
        return Result.Success();
    }
}
```

### Restore:
```csharp
entity.Restore(); // IsDeleted=false, DeletedAt=null, DeletedBy=null, IsActive=true
```

---

## 6. Repository Pattern

```csharp
// Domain layer — Interface:
public interface IVocabularyRepository
{
    Task<Vocabulary?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(List<Vocabulary> Items, int TotalCount)> GetPagedAsync(
        string languageCode, int page, int pageSize,
        string? searchTerm, string? level, Guid? topicId,
        CancellationToken ct = default);
    void Add(Vocabulary entity);
    void Update(Vocabulary entity);
    void Remove(Vocabulary entity, Guid? deletedBy = null, DateTime? deletedAt = null);
}

// Infrastructure layer — Implementation:
internal sealed class VocabularyRepository(CurriculumDbContext dbContext) : IVocabularyRepository
{
    // ... implement methods
    // KHÔNG gọi SaveChangesAsync — để UnitOfWorkBehavior xử lý
}
```

**Standard query methods:**
- `GetByIdAsync` — tracking (cho write operations)
- `GetByIdAsync` với `.AsNoTracking()` — cho read operations trong single query
- `GetPagedAsync` — paginated list với dynamic filters
- `SearchAsync` — text search với `ILike`

---

## 7. UnitOfWork Behavior

```csharp
// CurriculumUnitOfWorkBehavior — SaveChanges chỉ chạy sau Command, không phải Query
// Handler KHÔNG được gọi SaveChangesAsync — Behavior tự làm sau khi handler return
```

---

## 8. Migration Commands

```bash
# Add migration (chạy từ solution root):
dotnet ef migrations add {MigrationName} \
  --project src/Modules/Curriculum/EnglishPartner.Modules.Curriculum.Infrastructure \
  --startup-project src/Apps/EnglishPartner.Api \
  --context CurriculumDbContext

# Remove last migration:
dotnet ef migrations remove \
  --project src/Modules/Curriculum/... \
  --startup-project src/Apps/EnglishPartner.Api \
  --context CurriculumDbContext

# Apply to DB:
dotnet ef database update \
  --project src/Modules/Curriculum/... \
  --startup-project src/Apps/EnglishPartner.Api \
  --context CurriculumDbContext
```

> ⚠️ **KHÔNG** xóa files trong `Migrations/` folder. Nếu migration bị lỗi, dùng `remove` command.
