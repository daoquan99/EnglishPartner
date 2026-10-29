using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Grammar.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishLiveTutor.Infrastructure.Persistence.Configurations;

public sealed class GrammarStepEntityConfiguration : IEntityTypeConfiguration<GrammarStepEntity>
{
    public void Configure(EntityTypeBuilder<GrammarStepEntity> builder)
    {
        builder.ToTable("GrammarSteps");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Code).HasMaxLength(16).IsRequired();
        builder.Property(e => e.CefrLevel).HasMaxLength(8).IsRequired();
        builder.Property(e => e.Order).IsRequired();
        builder.Property(e => e.TitleVi).HasMaxLength(200).IsRequired();
        builder.Property(e => e.TitleEn).HasMaxLength(200).IsRequired();
        builder.Property(e => e.SummaryVi).HasMaxLength(500).IsRequired();
        builder.Property(e => e.TheoryJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(e => e.TheoryVersion).IsRequired();
        builder.Property(e => e.TheoryEditedByAdmin).IsRequired();
        builder.Property(e => e.IsLevelReview).IsRequired();
        builder.Property(e => e.IsPublished).IsRequired();
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.ConfigureAudit<GrammarStepEntity, Guid>();
        builder.ConfigureSoftDelete<GrammarStepEntity, Guid>();

        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => new { e.CefrLevel, e.Order }).IsUnique();
    }
}

public sealed class GrammarExerciseEntityConfiguration : IEntityTypeConfiguration<GrammarExerciseEntity>
{
    public void Configure(EntityTypeBuilder<GrammarExerciseEntity> builder)
    {
        builder.ToTable("GrammarExercises");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.StepId).IsRequired();
        builder.Property(e => e.Key).HasMaxLength(64);
        builder.Property(e => e.Type).HasConversion<int>().IsRequired();
        builder.Property(e => e.Difficulty).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Instructions).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.Focus).HasMaxLength(200).HasColumnType("nvarchar(200)");
        builder.Property(e => e.ContentJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(e => e.GradingDefinitionJson).HasColumnType("nvarchar(max)");
        builder.Property(e => e.MaxPoints).HasColumnType("decimal(6,2)").IsRequired();
        builder.Property(e => e.GradingMode).HasConversion<int>().IsRequired();
        builder.Property(e => e.Source).HasConversion<int>().IsRequired();
        builder.Property(e => e.OwnerUserId).HasMaxLength(128);
        builder.Property(e => e.AuthorUserId).HasMaxLength(128);
        builder.Property(e => e.Visibility).HasConversion<int>().IsRequired();
        builder.Property(e => e.Moderation).HasConversion<int>().IsRequired();
        builder.Property(e => e.RejectReason).HasMaxLength(500);
        builder.Property(e => e.ReviewedBy).HasMaxLength(128);
        builder.Property(e => e.ReviewedAtUtc);
        builder.Property(e => e.IsCore).IsRequired();
        builder.Property(e => e.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.ConfigureAudit<GrammarExerciseEntity, Guid>();
        builder.ConfigureSoftDelete<GrammarExerciseEntity, Guid>();

        builder.HasOne<GrammarStepEntity>()
            .WithMany()
            .HasForeignKey(e => e.StepId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.StepId, e.Source, e.Moderation, e.IsDeleted })
            .IncludeProperties(e => new { e.IsCore, e.OwnerUserId, e.Difficulty, e.Key, e.CreatedAtUtc });
        builder.HasIndex(e => new { e.OwnerUserId, e.StepId });
        builder.HasIndex(e => new { e.StepId, e.ContentHash });
        builder.HasIndex(e => new { e.StepId, e.Key })
            .IsUnique()
            .HasFilter("[Key] IS NOT NULL");
    }
}

public sealed class UserGrammarStepProgressEntityConfiguration : IEntityTypeConfiguration<UserGrammarStepProgressEntity>
{
    public void Configure(EntityTypeBuilder<UserGrammarStepProgressEntity> builder)
    {
        builder.ToTable("UserGrammarStepProgress");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.UserId).HasMaxLength(128).IsRequired();
        builder.Property(e => e.StepId).IsRequired();
        builder.Property(e => e.Status).HasConversion<int>().IsRequired();
        builder.Property(e => e.BestScore100).HasColumnType("decimal(5,2)").IsRequired();
        builder.Property(e => e.PassedBy).HasConversion<int>();
        builder.Property(e => e.UnlockedAtUtc).IsRequired();
        builder.Property(e => e.PassedAtUtc);

        builder.ConfigureAudit<UserGrammarStepProgressEntity, Guid>();
        builder.ConfigureSoftDelete<UserGrammarStepProgressEntity, Guid>();

        builder.HasOne<GrammarStepEntity>()
            .WithMany()
            .HasForeignKey(e => e.StepId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.UserId, e.StepId }).IsUnique();
    }
}

public sealed class GrammarAttemptEntityConfiguration : IEntityTypeConfiguration<GrammarAttemptEntity>
{
    public void Configure(EntityTypeBuilder<GrammarAttemptEntity> builder)
    {
        builder.ToTable("GrammarAttempts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.UserId).HasMaxLength(128).IsRequired();
        builder.Property(e => e.Kind).HasConversion<int>().IsRequired();
        builder.Property(e => e.StepId);
        builder.Property(e => e.CefrLevel).HasMaxLength(8).IsRequired();
        builder.Property(e => e.ExerciseIdsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(e => e.StartedAtUtc).IsRequired();
        builder.Property(e => e.CompletedAtUtc);
        builder.Property(e => e.Score100).HasColumnType("decimal(5,2)");
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.ConfigureAudit<GrammarAttemptEntity, Guid>();
        builder.ConfigureSoftDelete<GrammarAttemptEntity, Guid>();

        builder.HasOne<GrammarStepEntity>()
            .WithMany()
            .HasForeignKey(e => e.StepId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.UserId, e.StartedAtUtc });

        builder.HasMany(e => e.Answers)
            .WithOne()
            .HasForeignKey(a => a.AttemptId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Answers)
            .HasField("_answers")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class GrammarAttemptAnswerEntityConfiguration : IEntityTypeConfiguration<GrammarAttemptAnswerEntity>
{
    public void Configure(EntityTypeBuilder<GrammarAttemptAnswerEntity> builder)
    {
        builder.ToTable("GrammarAttemptAnswers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.AttemptId).IsRequired();
        builder.Property(e => e.ExerciseId).IsRequired();
        builder.Property(e => e.AnswerPayloadJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(e => e.Status).HasMaxLength(32).IsRequired();
        builder.Property(e => e.Score100).HasColumnType("decimal(5,2)");
        builder.Property(e => e.PointsEarned).HasColumnType("decimal(6,2)");
        builder.Property(e => e.MaxPoints).HasColumnType("decimal(6,2)").IsRequired();
        builder.Property(e => e.FeedbackJson).HasColumnType("nvarchar(max)");
        builder.Property(e => e.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(e => e.SubmittedAtUtc).IsRequired();

        builder.ConfigureAudit<GrammarAttemptAnswerEntity, Guid>();

        builder.HasIndex(e => new { e.AttemptId, e.ExerciseId }).IsUnique();
    }
}
