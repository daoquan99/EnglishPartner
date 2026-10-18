using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.Learning.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishLiveTutor.Infrastructure.Persistence.Configurations;

public sealed class DailyVocabularyPlanEntityConfiguration : IEntityTypeConfiguration<DailyVocabularyPlanEntity>
{
    public void Configure(EntityTypeBuilder<DailyVocabularyPlanEntity> builder)
    {
        builder.ToTable("DailyVocabularyPlans");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.UserId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.LearningDate).HasMaxLength(16).IsRequired();
        builder.Property(e => e.TimeZoneId).HasMaxLength(64).HasDefaultValue("Asia/Ho_Chi_Minh").IsRequired();
        builder.Property(e => e.Status).HasMaxLength(32).HasDefaultValue("Active").IsRequired();

        builder.Property(e => e.TargetCefrLevel).HasMaxLength(8);

        builder.ConfigureAudit<DailyVocabularyPlanEntity, Guid>(
            createdAtColumnName: "CreatedAt",
            updatedAtColumnName: "UpdatedAtUtc");
        builder.ConfigureSoftDelete<DailyVocabularyPlanEntity, Guid>();

        builder.HasIndex(e => new { e.UserId, e.LearningDate }).IsUnique();

        builder.HasMany(e => e.Items)
            .WithOne()
            .HasForeignKey(i => i.PlanId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Items)
            .HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class DailyVocabularyItemEntityConfiguration : IEntityTypeConfiguration<DailyVocabularyItemEntity>
{
    public void Configure(EntityTypeBuilder<DailyVocabularyItemEntity> builder)
    {
        builder.ToTable("DailyVocabularyItems");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.ItemType).HasMaxLength(32).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(32).IsRequired();

        builder.ConfigureAudit<DailyVocabularyItemEntity, Guid>();

        builder.HasIndex(e => new { e.PlanId, e.SenseId }).IsUnique();
        builder.HasIndex(e => new { e.PlanId, e.VocabularyId });
    }
}

public sealed class UserVocabularyProgressEntityConfiguration : IEntityTypeConfiguration<UserVocabularyProgressEntity>
{
    public void Configure(EntityTypeBuilder<UserVocabularyProgressEntity> builder)
    {
        builder.ToTable("UserVocabularyProgresses");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.UserId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.EaseFactor).HasColumnType("decimal(5,2)").HasDefaultValue(2.50m);
        builder.Property(e => e.LatestPronunciationScore).HasColumnType("decimal(5,2)");
        builder.Property(e => e.LearningStatus).HasMaxLength(32).HasDefaultValue("New").IsRequired();

        builder.ConfigureAudit<UserVocabularyProgressEntity, Guid>();
        builder.ConfigureSoftDelete<UserVocabularyProgressEntity, Guid>();

        builder.HasIndex(e => new { e.UserId, e.SenseId }).IsUnique();
        builder.HasIndex(e => new { e.UserId, e.NextReviewAt });
    }
}

public sealed class VocabularyReviewAttemptEntityConfiguration : IEntityTypeConfiguration<VocabularyReviewAttemptEntity>
{
    public void Configure(EntityTypeBuilder<VocabularyReviewAttemptEntity> builder)
    {
        builder.ToTable("VocabularyReviewAttempts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.UserId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.ReviewType).HasMaxLength(32).IsRequired();
        builder.Property(e => e.Rating).HasMaxLength(16).IsRequired();
        builder.Property(e => e.ClientRequestId).HasMaxLength(64);

        builder.ConfigureAudit<VocabularyReviewAttemptEntity, Guid>();
        builder.ConfigureSoftDelete<VocabularyReviewAttemptEntity, Guid>();

        builder.HasIndex(e => new { e.UserId, e.ClientRequestId })
            .IsUnique()
            .HasFilter("[ClientRequestId] IS NOT NULL");
    }
}

public sealed class UserLearningSettingsEntityConfiguration : IEntityTypeConfiguration<UserLearningSettingsEntity>
{
    public void Configure(EntityTypeBuilder<UserLearningSettingsEntity> builder)
    {
        builder.ToTable("UserLearningSettings");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.UserId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.DailyVocabularyTarget).IsRequired();
        builder.Property(e => e.CefrLevel).HasMaxLength(8).HasDefaultValue("B1").IsRequired();
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.ConfigureAudit<UserLearningSettingsEntity, Guid>();
        builder.ConfigureSoftDelete<UserLearningSettingsEntity, Guid>();

        builder.HasIndex(e => e.UserId).IsUnique();
    }
}
