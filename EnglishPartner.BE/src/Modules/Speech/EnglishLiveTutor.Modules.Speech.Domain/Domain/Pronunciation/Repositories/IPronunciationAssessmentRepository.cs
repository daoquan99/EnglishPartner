namespace EnglishLiveTutor.Domain.Pronunciation.Repositories;

public interface IPronunciationAssessmentRepository
{
    Task AddRecordingAsync(AudioRecordingEntity recording, CancellationToken ct = default);
    Task AddAssessmentAsync(PronunciationAssessmentEntity assessment, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
