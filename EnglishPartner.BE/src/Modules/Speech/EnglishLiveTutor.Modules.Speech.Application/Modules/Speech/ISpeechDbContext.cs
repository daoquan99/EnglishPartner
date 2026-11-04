using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Modules.Speech;

public interface ISpeechDbContext : IModuleDbContext
{
    DbSet<ScoringRubricEntity> ScoringRubrics { get; }
    DbSet<ScoringRubricCriterionEntity> ScoringRubricCriteria { get; }
    DbSet<AudioRecordingEntity> AudioRecordings { get; }
    DbSet<PronunciationAssessmentEntity> PronunciationAssessments { get; }
    DbSet<PronunciationCriterionScoreEntity> PronunciationCriterionScores { get; }
    DbSet<GeneratedPronunciationAudioEntity> GeneratedPronunciationAudios { get; }
}
