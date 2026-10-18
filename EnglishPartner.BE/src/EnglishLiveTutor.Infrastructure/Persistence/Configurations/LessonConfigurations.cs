using EnglishLiveTutor.Domain.LessonAttempts;
using EnglishLiveTutor.Domain.LessonAttempts.Entities;
using EnglishLiveTutor.Domain.Lessons;
using EnglishLiveTutor.Domain.Lessons.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishLiveTutor.Infrastructure.Persistence.Configurations;

public sealed class LessonEntityConfiguration : IEntityTypeConfiguration<LessonEntity>
{
    public void Configure(EntityTypeBuilder<LessonEntity> builder)
    {
        builder.ToTable("Lessons");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Title).HasMaxLength(256).IsRequired();
        builder.Property(e => e.CefrLevel).HasMaxLength(8).IsRequired();
        builder.Property(e => e.Topic).HasMaxLength(128).IsRequired();
        builder.Property(e => e.LearningObjectivesJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(e => e.TargetVocabularyJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(e => e.TargetGrammarJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(e => e.Content).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(e => e.EstimatedMinutes).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.Version).IsRequired();
        builder.Property(e => e.ParentLessonId);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.ConfigureAudit<LessonEntity, Guid>();
        builder.ConfigureSoftDelete<LessonEntity, Guid>();

        builder.HasIndex(e => e.CefrLevel);
        builder.HasIndex(e => e.Topic);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.Id, e.Version });

        builder.HasMany(e => e.Exercises)
            .WithOne()
            .HasForeignKey(x => x.LessonId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Exercises)
            .HasField("_exercises")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ExerciseEntityConfiguration : IEntityTypeConfiguration<ExerciseEntity>
{
    public void Configure(EntityTypeBuilder<ExerciseEntity> builder)
    {
        builder.ToTable("Exercises");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.LessonId).IsRequired();
        builder.Property(e => e.Key).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Type).HasConversion<int>().IsRequired();
        builder.Property(e => e.Title).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Instructions).HasMaxLength(2000).IsRequired();
        builder.Property(e => e.Order).IsRequired();
        builder.Property(e => e.MaxPoints).HasColumnType("decimal(6,2)").IsRequired();
        builder.Property(e => e.GradingMode).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.SkillTagsJson).HasColumnType("nvarchar(max)");
        builder.Property(e => e.ContentJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(e => e.GradingDefinitionJson).HasColumnType("nvarchar(max)");

        builder.ConfigureAudit<ExerciseEntity, Guid>();

        builder.HasIndex(e => new { e.LessonId, e.Key }).IsUnique();
        builder.HasIndex(e => new { e.LessonId, e.Order });
    }
}

public sealed class LessonAttemptEntityConfiguration : IEntityTypeConfiguration<LessonAttemptEntity>
{
    public void Configure(EntityTypeBuilder<LessonAttemptEntity> builder)
    {
        builder.ToTable("LessonAttempts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.LessonId).IsRequired();
        builder.Property(e => e.UserId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.PinnedLessonVersion).IsRequired();
        builder.Property(e => e.AttemptRevision).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.StartedAtUtc).IsRequired();
        builder.Property(e => e.CompletedAtUtc);
        builder.Property(e => e.CurrentSubtotalPoints).HasColumnType("decimal(6,2)").IsRequired();
        builder.Property(e => e.TotalMaxPoints).HasColumnType("decimal(6,2)").IsRequired();
        builder.Property(e => e.FinalScore100).HasColumnType("decimal(5,2)");

        builder.ConfigureAudit<LessonAttemptEntity, Guid>();
        builder.ConfigureSoftDelete<LessonAttemptEntity, Guid>();

        builder.HasIndex(e => new { e.UserId, e.LessonId });
        builder.HasIndex(e => e.Status);

        builder.HasMany(e => e.Answers)
            .WithOne()
            .HasForeignKey(a => a.AttemptId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Answers)
            .HasField("_answers")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ExerciseAnswerEntityConfiguration : IEntityTypeConfiguration<ExerciseAnswerEntity>
{
    public void Configure(EntityTypeBuilder<ExerciseAnswerEntity> builder)
    {
        builder.ToTable("ExerciseAnswers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.AttemptId).IsRequired();
        builder.Property(e => e.ExerciseId).IsRequired();
        builder.Property(e => e.SubQuestionId);
        builder.Property(e => e.GradingMode).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.MaxPoints).HasColumnType("decimal(6,2)").IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.Score100).HasColumnType("decimal(5,2)");
        builder.Property(e => e.PointsEarned).HasColumnType("decimal(6,2)");
        builder.Property(e => e.AnswerPayloadJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(e => e.GradingResultJson).HasColumnType("nvarchar(max)");
        builder.Property(e => e.LeaseExpiresAtUtc);
        builder.Property(e => e.SubmittedAtUtc).IsRequired();
        builder.Property(e => e.GradedAtUtc);

        builder.ConfigureAudit<ExerciseAnswerEntity, Guid>();

        builder.HasIndex(e => new { e.AttemptId, e.ExerciseId, e.SubQuestionId }).IsUnique();
        builder.HasIndex(e => e.Status);
    }
}
