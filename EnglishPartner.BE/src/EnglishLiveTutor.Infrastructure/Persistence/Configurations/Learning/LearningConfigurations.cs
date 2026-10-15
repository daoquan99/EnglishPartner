using EnglishLiveTutor.Domain.Learning.DailyVocabularyPlans;
using EnglishLiveTutor.Domain.Learning.DailyVocabularyPlans.Entities;
using EnglishLiveTutor.Domain.Learning.ReviewAttempts;
using EnglishLiveTutor.Domain.Learning.UserVocabularyProgress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishLiveTutor.Infrastructure.Persistence.Configurations.Learning;

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

        builder.ConfigureAudit<VocabularyReviewAttemptEntity, Guid>();
        builder.ConfigureSoftDelete<VocabularyReviewAttemptEntity, Guid>();
    }
}
