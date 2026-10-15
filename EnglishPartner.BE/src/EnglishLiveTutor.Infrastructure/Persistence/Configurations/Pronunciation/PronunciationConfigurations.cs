using EnglishLiveTutor.Domain.Pronunciation.Assessments;
using EnglishLiveTutor.Domain.Pronunciation.Assessments.Entities;
using EnglishLiveTutor.Domain.Pronunciation.AudioRecordings;
using EnglishLiveTutor.Domain.Pronunciation.GeneratedAudio;
using EnglishLiveTutor.Domain.Pronunciation.ScoringRubrics;
using EnglishLiveTutor.Domain.Pronunciation.ScoringRubrics.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishLiveTutor.Infrastructure.Persistence.Configurations.Pronunciation;

public sealed class ScoringRubricEntityConfiguration : IEntityTypeConfiguration<ScoringRubricEntity>
{
    public void Configure(EntityTypeBuilder<ScoringRubricEntity> builder)
    {
        builder.ToTable("ScoringRubrics");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Code).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(160).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.AssessmentType).HasMaxLength(32).HasDefaultValue("Pronunciation").IsRequired();

        builder.ConfigureAudit<ScoringRubricEntity, Guid>(
            createdAtColumnName: "CreatedAt",
            updatedAtColumnName: "UpdatedAt");
        builder.ConfigureSoftDelete<ScoringRubricEntity, Guid>();

        builder.HasIndex(e => new { e.Code, e.Version }).IsUnique();

        builder.HasMany(e => e.Criteria)
            .WithOne()
            .HasForeignKey(c => c.RubricId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Criteria)
            .HasField("_criteria")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ScoringRubricCriterionEntityConfiguration : IEntityTypeConfiguration<ScoringRubricCriterionEntity>
{
    public void Configure(EntityTypeBuilder<ScoringRubricCriterionEntity> builder)
    {
        builder.ToTable("ScoringRubricCriteria");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Code).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(128).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.Weight).HasColumnType("decimal(5,2)").IsRequired();
        builder.Property(e => e.MinimumScore).HasColumnType("decimal(5,2)").HasDefaultValue(0m);
        builder.Property(e => e.MaximumScore).HasColumnType("decimal(5,2)").HasDefaultValue(100m);

        builder.ConfigureAudit<ScoringRubricCriterionEntity, Guid>();
    }
}

public sealed class AudioRecordingEntityConfiguration : IEntityTypeConfiguration<AudioRecordingEntity>
{
    public void Configure(EntityTypeBuilder<AudioRecordingEntity> builder)
    {
        builder.ToTable("AudioRecordings");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.UserId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.StorageKey).HasMaxLength(256).IsRequired();
        builder.Property(e => e.ContentType).HasMaxLength(64).HasDefaultValue("audio/webm");
        builder.Property(e => e.DeletionStatus).HasMaxLength(32).HasDefaultValue("Retained");

        builder.ConfigureAudit<AudioRecordingEntity, Guid>(
            createdAtColumnName: "CreatedAt",
            updatedAtColumnName: "UpdatedAtUtc");
        builder.ConfigureSoftDelete<AudioRecordingEntity, Guid>();
    }
}

public sealed class PronunciationAssessmentEntityConfiguration : IEntityTypeConfiguration<PronunciationAssessmentEntity>
{
    public void Configure(EntityTypeBuilder<PronunciationAssessmentEntity> builder)
    {
        builder.ToTable("PronunciationAssessments");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.UserId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.TargetType).HasMaxLength(32).HasDefaultValue("Word").IsRequired();
        builder.Property(e => e.ReferenceText).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.Accent).HasMaxLength(16).HasDefaultValue("US").IsRequired();
        builder.Property(e => e.ModelCode).HasMaxLength(128).IsRequired();
        builder.Property(e => e.RecognizedText).HasMaxLength(1000);
        builder.Property(e => e.OverallScore).HasColumnType("decimal(5,2)");
        builder.Property(e => e.Confidence).HasColumnType("decimal(5,2)");
        builder.Property(e => e.Status).HasMaxLength(32).HasDefaultValue("Completed").IsRequired();
        builder.Property(e => e.ErrorCode).HasMaxLength(64);

        builder.ConfigureAudit<PronunciationAssessmentEntity, Guid>(
            createdAtColumnName: "CreatedAt",
            updatedAtColumnName: "UpdatedAtUtc");
        builder.ConfigureSoftDelete<PronunciationAssessmentEntity, Guid>();

        builder.HasIndex(e => new { e.UserId, e.VocabularyId, e.CreatedAtUtc })
            .HasDatabaseName("IX_PronunciationAssessments_UserId_VocabularyId_CreatedAt");

        builder.HasMany(e => e.CriterionScores)
            .WithOne()
            .HasForeignKey(c => c.AssessmentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.CriterionScores)
            .HasField("_criterionScores")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class PronunciationCriterionScoreEntityConfiguration : IEntityTypeConfiguration<PronunciationCriterionScoreEntity>
{
    public void Configure(EntityTypeBuilder<PronunciationCriterionScoreEntity> builder)
    {
        builder.ToTable("PronunciationCriterionScores");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.CriterionCode).HasMaxLength(64).IsRequired();
        builder.Property(e => e.CriterionName).HasMaxLength(128).IsRequired();
        builder.Property(e => e.CriterionDescriptionSnapshot).HasMaxLength(500);
        builder.Property(e => e.WeightSnapshot).HasColumnType("decimal(5,2)").IsRequired();
        builder.Property(e => e.RawScore).HasColumnType("decimal(5,2)").IsRequired();
        builder.Property(e => e.WeightedScore).HasColumnType("decimal(5,2)").IsRequired();
        builder.Property(e => e.Feedback).HasMaxLength(500);

        builder.ConfigureAudit<PronunciationCriterionScoreEntity, Guid>();
    }
}

public sealed class GeneratedPronunciationAudioConfiguration : IEntityTypeConfiguration<GeneratedPronunciationAudioEntity>
{
    public void Configure(EntityTypeBuilder<GeneratedPronunciationAudioEntity> builder)
    {
        builder.ToTable("GeneratedPronunciationAudios");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.TargetType).HasMaxLength(32).IsRequired();
        builder.Property(e => e.TargetId).IsRequired();
        builder.Property(e => e.VoiceName).HasMaxLength(64).IsRequired();
        builder.Property(e => e.SpeechSpeed).HasColumnType("decimal(3,2)").IsRequired();
        builder.Property(e => e.Accent).HasMaxLength(16).HasDefaultValue("US").IsRequired();
        builder.Property(e => e.ModelCode).HasMaxLength(128).IsRequired();
        builder.Property(e => e.TextSnapshot).HasMaxLength(2000).IsRequired();
        builder.Property(e => e.TextHash).HasMaxLength(64).IsRequired();
        builder.Property(e => e.AudioData).HasColumnType("varbinary(max)").IsRequired();
        builder.Property(e => e.MimeType).HasMaxLength(64).HasDefaultValue("audio/wav").IsRequired();
        builder.Property(e => e.SampleRate).HasDefaultValue(24000).IsRequired();
        builder.Property(e => e.DurationMs).IsRequired();
        builder.Property(e => e.SystemPromptUsed).HasMaxLength(4000).IsRequired();
        builder.Property(e => e.IsValid).HasDefaultValue(true).IsRequired();

        builder.ConfigureAudit<GeneratedPronunciationAudioEntity, Guid>();
        builder.ConfigureSoftDelete<GeneratedPronunciationAudioEntity, Guid>();

        builder.HasIndex(e => new { e.TargetType, e.VoiceName, e.SpeechSpeed, e.TargetId });

        builder.HasIndex(e => new { e.TargetType, e.TargetId, e.VoiceName, e.SpeechSpeed, e.Accent, e.ModelCode, e.TextHash })
            .HasDatabaseName("IX_GeneratedPronunciationAudios_Lookup");
    }
}
