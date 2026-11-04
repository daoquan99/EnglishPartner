using EnglishLiveTutor.Application.Modules.Speech;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Repositories;

public sealed class AudioRecordingRepository : IAudioRecordingRepository
{
    private readonly ISpeechDbContext _db;

    public AudioRecordingRepository(ISpeechDbContext db)
    {
        _db = db;
    }

    public Task<AudioRecordingEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.AudioRecordings.FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public Task<AudioRecordingEntity?> GetOwnedByIdAsync(
        Guid id,
        string userId,
        CancellationToken ct = default)
    {
        string normalizedUserId = userId.ToLowerInvariant();
        return _db.AudioRecordings.FirstOrDefaultAsync(
            r => r.Id == id && r.UserId.ToLower() == normalizedUserId,
            ct);
    }

    public void Add(AudioRecordingEntity recording)
    {
        _db.AudioRecordings.Add(recording);
    }
}
