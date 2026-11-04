using EnglishLiveTutor.Infrastructure.Persistence.Modules;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence;

public interface IDatabaseMigrationRunner
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
}

public interface IDatabaseContextMigrator<TContext>
    where TContext : DbContext
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
}

public sealed class DatabaseMigrationRunner(
    IDatabaseContextMigrator<SharedDbContext> sharedMigrator,
    IDatabaseContextMigrator<IdentityDbContext> identityMigrator,
    IDatabaseContextMigrator<CatalogDbContext> catalogMigrator,
    IDatabaseContextMigrator<SpeechDbContext> speechMigrator,
    IDatabaseContextMigrator<LessonsDbContext> lessonsMigrator,
    IDatabaseContextMigrator<GrammarDbContext> grammarMigrator,
    IDatabaseContextMigrator<VocabularyDbContext> vocabularyMigrator,
    IDatabaseContextMigrator<LiveTutorDbContext> liveTutorMigrator)
    : IDatabaseMigrationRunner
{
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await sharedMigrator.MigrateAsync(cancellationToken);
        await identityMigrator.MigrateAsync(cancellationToken);
        await catalogMigrator.MigrateAsync(cancellationToken);
        await speechMigrator.MigrateAsync(cancellationToken);
        await lessonsMigrator.MigrateAsync(cancellationToken);
        await grammarMigrator.MigrateAsync(cancellationToken);
        await vocabularyMigrator.MigrateAsync(cancellationToken);
        await liveTutorMigrator.MigrateAsync(cancellationToken);
    }
}

internal sealed class DatabaseContextMigrator<TContext>(
    IDbContextFactory<TContext> contextFactory)
    : IDatabaseContextMigrator<TContext>
    where TContext : DbContext
{
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
    }
}
