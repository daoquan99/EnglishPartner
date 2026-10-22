using EnglishLiveTutor.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishLiveTutor.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyRecordEntityConfiguration : IEntityTypeConfiguration<IdempotencyRecordEntity>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecordEntity> builder)
    {
        builder.ToTable("IdempotencyRecords");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.UserId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.OperationType).HasMaxLength(64).IsRequired();
        builder.Property(e => e.ResourceScope).HasMaxLength(128).IsRequired();
        builder.Property(e => e.OperationKey).HasMaxLength(128).IsRequired();
        builder.Property(e => e.RequestFingerprint).HasMaxLength(128).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(32).IsRequired();
        builder.Property(e => e.ResponseStatusCode).IsRequired();
        builder.Property(e => e.ResponsePayloadJson).HasColumnType("nvarchar(max)");
        builder.Property(e => e.LocationHeader).HasMaxLength(256);
        builder.Property(e => e.CreatedAtUtc).IsRequired();
        builder.Property(e => e.CompletedAtUtc);
        builder.Property(e => e.ExpiresAtUtc).IsRequired();
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasIndex(e => new { e.UserId, e.OperationType, e.ResourceScope, e.OperationKey })
            .IsUnique()
            .HasDatabaseName("IX_IdempotencyRecords_Scope_Key");

        builder.HasIndex(e => e.ExpiresAtUtc);
    }
}
