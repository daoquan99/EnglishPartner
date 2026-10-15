namespace EnglishLiveTutor.Application.Features.Vocabulary.Ports;

/// <summary>
/// Writes failed vocabulary generation audit runs in an isolated persistence context
/// so that an aborted/failed generation never flushes partially staged vocabulary entities.
/// </summary>
public interface IVocabularyGenerationFailureAuditWriter
{
    Task RecordFailureAsync(
        string userId,
        string modelCode,
        string promptVersion,
        int requestedCount,
        string errorMessage,
        CancellationToken ct = default);
}
