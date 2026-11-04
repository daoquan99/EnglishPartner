using EnglishLiveTutor.Infrastructure.Persistence.Configurations;
using EnglishLiveTutor.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence;

public class SharedDbContext : DbContext
{
    public SharedDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<IdempotencyRecordEntity> IdempotencyRecords => Set<IdempotencyRecordEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ModuleSchemas.Shared);
        modelBuilder.ApplyConfiguration(new IdempotencyRecordEntityConfiguration());
    }
}
