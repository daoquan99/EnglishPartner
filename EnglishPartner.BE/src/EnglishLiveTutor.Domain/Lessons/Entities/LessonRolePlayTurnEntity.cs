using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Lessons.Entities;

public sealed class LessonRolePlayTurnEntity : Entity<Guid>
{
    public Guid SessionId { get; private set; }
    public int SequenceIndex { get; private set; }
    public int TurnIndex { get; private set; }
    public string Role { get; private set; } = string.Empty; // "assistant" or "user"
    public string Message { get; private set; } = string.Empty;
    public Guid? AudioAssetId { get; private set; }

    private LessonRolePlayTurnEntity()
    {
    }

    public LessonRolePlayTurnEntity(
        Guid sessionId,
        int sequenceIndex,
        int turnIndex,
        string role,
        string message,
        Guid? audioAssetId = null,
        DateTimeOffset? createdAtUtc = null,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("SessionId cannot be empty.", nameof(sessionId));
        if (sequenceIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(sequenceIndex), "SequenceIndex must be non-negative.");
        if (turnIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(turnIndex), "TurnIndex must be non-negative.");
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role is required.", nameof(role));

        SessionId = sessionId;
        SequenceIndex = sequenceIndex;
        TurnIndex = turnIndex;
        Role = role.Trim().ToLowerInvariant();
        Message = message ?? string.Empty;
        AudioAssetId = audioAssetId;
        CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow;
    }
}
