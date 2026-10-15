using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;
using EnglishLiveTutor.Infrastructure.Persistence;

namespace EnglishLiveTutor.Infrastructure.Repositories;

public sealed class PronunciationAssessmentRepository : IPronunciationAssessmentRepository
{
    private readonly AppDbContext _db;

    public PronunciationAssessmentRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task AddRecordingAsync(AudioRecordingEntity recording, CancellationToken ct = default)
    {
        _db.AudioRecordings.Add(recording);
        return Task.CompletedTask;
    }

    public Task AddAssessmentAsync(PronunciationAssessmentEntity assessment, CancellationToken ct = default)
    {
        _db.PronunciationAssessments.Add(assessment);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
