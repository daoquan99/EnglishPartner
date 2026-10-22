using EnglishLiveTutor.Domain.Common.Primitives;
using EnglishLiveTutor.Domain.Lessons.Entities;

namespace EnglishLiveTutor.Domain.Lessons;

public sealed class LessonRolePlaySessionEntity : AggregateRoot<Guid>
{
    public string UserId { get; private set; } = string.Empty;
    public string Scenario { get; private set; } = string.Empty;
    public string AiRole { get; private set; } = string.Empty;
    public string InitialGreeting { get; private set; } = string.Empty;
    public int MaxTurns { get; private set; } = 6;
    public int CurrentTurnIndex { get; private set; }
    public string Status { get; private set; } = "Active";
    public DateTimeOffset? EndedAtUtc { get; private set; }
    public DateTimeOffset? ClaimedUntilUtc { get; private set; }
    public int Version { get; private set; } = 1;
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private readonly List<LessonRolePlayTurnEntity> _turns = new();
    public IReadOnlyCollection<LessonRolePlayTurnEntity> Turns => _turns.AsReadOnly();

    private LessonRolePlaySessionEntity()
    {
    }

    public LessonRolePlaySessionEntity(
        Guid id,
        string userId,
        string scenario,
        string aiRole,
        string initialGreeting,
        int maxTurns,
        DateTimeOffset now)
        : base(id == Guid.Empty ? Guid.NewGuid() : id)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(scenario))
            throw new ArgumentException("Scenario is required.", nameof(scenario));

        string role = string.IsNullOrWhiteSpace(aiRole) ? "AI Partner" : aiRole.Trim();
        string greeting = string.IsNullOrWhiteSpace(initialGreeting)
            ? $"Hello! I am your {role}. How can I help you today?"
            : initialGreeting.Trim();
        int safeMaxTurns = maxTurns <= 0 ? 6 : Math.Clamp(maxTurns, 1, 50);

        UserId = userId.Trim();
        Scenario = scenario.Trim();
        AiRole = role;
        InitialGreeting = greeting;
        MaxTurns = safeMaxTurns;
        CurrentTurnIndex = 0;
        Status = "Active";
        Version = 1;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;

        // Turn 0: Initial assistant greeting
        _turns.Add(new LessonRolePlayTurnEntity(
            sessionId: Id,
            sequenceIndex: 0,
            turnIndex: 0,
            role: "assistant",
            message: greeting,
            audioAssetId: null,
            createdAtUtc: now));
    }

    public static LessonRolePlaySessionEntity Start(
        string userId,
        string scenario,
        string? aiRole,
        string? initialGreeting,
        int maxTurns,
        DateTimeOffset now,
        Guid? id = null)
    {
        return new LessonRolePlaySessionEntity(
            id: id ?? Guid.NewGuid(),
            userId: userId,
            scenario: scenario,
            aiRole: aiRole ?? string.Empty,
            initialGreeting: initialGreeting ?? string.Empty,
            maxTurns: maxTurns,
            now: now);
    }

    public void ReserveTurn(int clientTurnIndex, DateTimeOffset now, TimeSpan leaseDuration)
    {
        EnsureActive();

        if (clientTurnIndex != CurrentTurnIndex + 1)
        {
            throw new InvalidOperationException(
                $"TURN_INDEX_CONFLICT: Expected turnIndex {CurrentTurnIndex + 1}, received {clientTurnIndex}.");
        }

        if (CurrentTurnIndex >= MaxTurns)
        {
            throw new InvalidOperationException("MaxTurnsReached: This conversation session has completed its allotted turns.");
        }

        if (ClaimedUntilUtc.HasValue && ClaimedUntilUtc.Value > now)
        {
            throw new InvalidOperationException("TURN_IN_PROGRESS: Another turn is currently being generated for this session.");
        }

        ClaimedUntilUtc = now.Add(leaseDuration);
        UpdatedAtUtc = now;
        Version++;
    }

    public void ReleaseReservation(DateTimeOffset now)
    {
        ClaimedUntilUtc = null;
        UpdatedAtUtc = now;
        Version++;
    }

    public void CommitTurn(
        int clientTurnIndex,
        string userMessage,
        Guid? userAudioAssetId,
        string aiReply,
        DateTimeOffset now)
    {
        EnsureActive();

        if (clientTurnIndex != CurrentTurnIndex + 1)
        {
            throw new InvalidOperationException(
                $"TURN_INDEX_CONFLICT: Expected turnIndex {CurrentTurnIndex + 1}, received {clientTurnIndex}.");
        }

        if (CurrentTurnIndex >= MaxTurns)
        {
            throw new InvalidOperationException("MaxTurnsReached: This conversation session has completed its allotted turns.");
        }

        int nextSeq = _turns.Count;
        _turns.Add(new LessonRolePlayTurnEntity(
            sessionId: Id,
            sequenceIndex: nextSeq,
            turnIndex: clientTurnIndex,
            role: "user",
            message: userMessage ?? string.Empty,
            audioAssetId: userAudioAssetId,
            createdAtUtc: now));

        _turns.Add(new LessonRolePlayTurnEntity(
            sessionId: Id,
            sequenceIndex: nextSeq + 1,
            turnIndex: clientTurnIndex,
            role: "assistant",
            message: aiReply ?? string.Empty,
            audioAssetId: null,
            createdAtUtc: now));

        CurrentTurnIndex = clientTurnIndex;
        ClaimedUntilUtc = null;
        UpdatedAtUtc = now;
        Version++;

        if (CurrentTurnIndex >= MaxTurns)
        {
            Status = "SessionEnded";
            EndedAtUtc = now;
        }
    }

    public void EndSession(DateTimeOffset now)
    {
        if (Status == "SessionEnded")
        {
            return; // Idempotent
        }

        Status = "SessionEnded";
        EndedAtUtc = now;
        ClaimedUntilUtc = null;
        UpdatedAtUtc = now;
        Version++;
    }

    private void EnsureActive()
    {
        if (Status != "Active")
        {
            throw new InvalidOperationException($"Session is not active. Current status: '{Status}'.");
        }
    }
}
