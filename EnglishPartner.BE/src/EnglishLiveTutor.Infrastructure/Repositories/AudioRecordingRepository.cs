using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Repositories;

public sealed class AudioRecordingRepository : IAudioRecordingRepository
{
    private readonly AppDbContext _db;

    public AudioRecordingRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<AudioRecordingEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.AudioRecordings.FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public void Add(AudioRecordingEntity recording)
    {
        _db.AudioRecordings.Add(recording);
    }
}
