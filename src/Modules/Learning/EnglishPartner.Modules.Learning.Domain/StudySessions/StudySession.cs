using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Learning.Domain.StudySessions;

public enum SessionStatus
{
    InProgress = 0,
    Completed = 1,
    Abandoned = 2
}

/// <summary>
/// Represents to a practice or spaced repetition session.
/// </summary>
public sealed class StudySession : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string SessionTitle { get; private set; } = null!;
    public DateTime StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public int Score { get; private set; }
    public int TotalItems { get; private set; }
    public SessionStatus Status { get; private set; }
    public DateTime? LastActivityTime { get; private set; }

    private StudySession() { }

    public static StudySession Start(Guid userId, string sessionTitle, int totalItems)
    {
        var now = DateTime.UtcNow;
        return new StudySession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionTitle = sessionTitle,
            StartTime = now,
            TotalItems = totalItems,
            Status = SessionStatus.InProgress,
            Score = 0,
            LastActivityTime = now
        };
    }

    public void Ping(DateTime pingTime)
    {
        if (Status != SessionStatus.InProgress) return;
        LastActivityTime = pingTime;
    }

    public void Complete(int score)
    {
        if (Status != SessionStatus.InProgress) return;
        Score = score;
        EndTime = DateTime.UtcNow;
        Status = SessionStatus.Completed;
    }

    public void Abandon()
    {
        if (Status != SessionStatus.InProgress) return;
        EndTime = DateTime.UtcNow;
        Status = SessionStatus.Abandoned;
    }
}
