using EnglishLiveTutor.Domain.Common.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishLiveTutor.Infrastructure.Persistence.Configurations;

internal static class EntityConfigurationExtensions
{
    public static void ConfigureAudit<TEntity, TId>(
        this EntityTypeBuilder<TEntity> builder,
        string createdAtColumnName = "CreatedAtUtc",
        string updatedAtColumnName = "UpdatedAtUtc")
        where TEntity : Entity<TId>
        where TId : notnull
    {
        builder.Property(e => e.CreatedAtUtc)
            .HasColumnName(createdAtColumnName)
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasColumnName("CreatedBy")
            .HasMaxLength(64);

        builder.Property(e => e.UpdatedAtUtc)
            .HasColumnName(updatedAtColumnName);

        builder.Property(e => e.UpdatedBy)
            .HasColumnName("UpdatedBy")
            .HasMaxLength(64);
    }

    public static void ConfigureSoftDelete<TAggregate, TId>(
        this EntityTypeBuilder<TAggregate> builder)
        where TAggregate : AggregateRoot<TId>
        where TId : notnull
    {
        builder.Property(e => e.IsDeleted)
            .HasColumnName("IsDeleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.DeletedAtUtc)
            .HasColumnName("DeletedAtUtc");

        builder.Property(e => e.DeletedBy)
            .HasColumnName("DeletedBy")
            .HasMaxLength(64);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
