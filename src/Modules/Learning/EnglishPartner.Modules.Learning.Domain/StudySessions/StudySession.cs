using EnglishPartner.SharedKernel.Domain;
using EnglishPartner.Modules.Learning.Domain.StudySessions.Events;

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

    public static StudySession Start(Guid userId, string sessionTitle, int totalItems, DateTime utcNow)
    {
        return new StudySession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionTitle = sessionTitle,
            StartTime = utcNow,
            TotalItems = totalItems,
            Status = SessionStatus.InProgress,
            Score = 0,
            LastActivityTime = utcNow
        };
    }

    public void Ping(DateTime pingTime)
    {
        if (Status != SessionStatus.InProgress) return;
        LastActivityTime = pingTime;
    }

    public void Complete(int score, DateTime utcNow)
    {
        if (Status != SessionStatus.InProgress) return;
        Score = score;
        EndTime = utcNow;
        Status = SessionStatus.Completed;

        RaiseDomainEvent(new StudySessionCompletedDomainEvent(Id, UserId, Score, TotalItems));
    }

    public void Abandon(DateTime utcNow)
    {
        if (Status != SessionStatus.InProgress) return;
        EndTime = utcNow;
        Status = SessionStatus.Abandoned;
    }
}
