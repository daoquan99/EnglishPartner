using EnglishLiveTutor.Application.Modules.Speech;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Entities;
using EnglishLiveTutor.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence.Modules;

public sealed class SpeechDbContext(DbContextOptions<SpeechDbContext> options)
    : DbContext(options), ISpeechDbContext
{
    public DbSet<ScoringRubricEntity> ScoringRubrics => Set<ScoringRubricEntity>();
    public DbSet<ScoringRubricCriterionEntity> ScoringRubricCriteria => Set<ScoringRubricCriterionEntity>();
    public DbSet<AudioRecordingEntity> AudioRecordings => Set<AudioRecordingEntity>();
    public DbSet<PronunciationAssessmentEntity> PronunciationAssessments => Set<PronunciationAssessmentEntity>();
    public DbSet<PronunciationCriterionScoreEntity> PronunciationCriterionScores => Set<PronunciationCriterionScoreEntity>();
    public DbSet<GeneratedPronunciationAudioEntity> GeneratedPronunciationAudios => Set<GeneratedPronunciationAudioEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ModuleSchemas.Speech);
        modelBuilder.ApplyConfiguration(new ScoringRubricEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ScoringRubricCriterionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AudioRecordingEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PronunciationAssessmentEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PronunciationCriterionScoreEntityConfiguration());
        modelBuilder.ApplyConfiguration(new GeneratedPronunciationAudioConfiguration());
    }
}
