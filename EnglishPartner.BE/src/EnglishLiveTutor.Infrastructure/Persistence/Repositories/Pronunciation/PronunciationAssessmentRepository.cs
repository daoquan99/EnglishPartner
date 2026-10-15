using EnglishLiveTutor.Domain.Pronunciation.Assessments;
using EnglishLiveTutor.Domain.Pronunciation.AudioRecordings;

namespace EnglishLiveTutor.Infrastructure.Persistence.Repositories.Pronunciation;

public sealed class PronunciationAssessmentRepository : IPronunciationAssessmentRepository
{
    private readonly AppDbContext _db;

    public PronunciationAssessmentRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task AddAudioRecordingAsync(AudioRecordingEntity recording, CancellationToken ct = default)
    {
        _db.AudioRecordings.Add(recording);
        return Task.CompletedTask;
    }

    public Task AddAsync(PronunciationAssessmentEntity assessment, CancellationToken ct = default)
    {
        _db.PronunciationAssessments.Add(assessment);
        return Task.CompletedTask;
    }
}
