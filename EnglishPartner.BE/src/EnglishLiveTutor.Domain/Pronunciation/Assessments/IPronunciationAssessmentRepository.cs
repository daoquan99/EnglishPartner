using EnglishLiveTutor.Domain.Pronunciation.AudioRecordings;

namespace EnglishLiveTutor.Domain.Pronunciation.Assessments;

public interface IPronunciationAssessmentRepository
{
    Task AddAudioRecordingAsync(AudioRecordingEntity recording, CancellationToken ct = default);
    Task AddAsync(PronunciationAssessmentEntity assessment, CancellationToken ct = default);
}
