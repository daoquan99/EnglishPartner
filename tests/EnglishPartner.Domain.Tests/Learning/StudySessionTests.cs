using EnglishPartner.Modules.Learning.Domain.StudySessions;
using EnglishPartner.Modules.Learning.Domain.StudySessions.Events;

namespace EnglishPartner.Domain.Tests.Learning;

public sealed class StudySessionTests
{
    private static StudySession StartNew() =>
        StudySession.Start(Guid.NewGuid(), "Morning Review", totalItems: 10);

    // ── Start ─────────────────────────────────────────────────────────────

    [Fact]
    public void Start_ShouldCreate_WithInProgressStatus()
    {
        var session = StartNew();

        session.Status.Should().Be(SessionStatus.InProgress);
        session.Score.Should().Be(0);
        session.EndTime.Should().BeNull();
    }

    [Fact]
    public void Start_ShouldSetLastActivityTime()
    {
        var before  = DateTime.UtcNow.AddSeconds(-1);
        var session = StartNew();

        session.LastActivityTime.Should().BeAfter(before);
    }

    // ── Complete ──────────────────────────────────────────────────────────

    [Fact]
    public void Complete_ShouldSetStatusCompleted_AndRecordScore()
    {
        var session = StartNew();

        session.Complete(85);

        session.Status.Should().Be(SessionStatus.Completed);
        session.Score.Should().Be(85);
        session.EndTime.Should().NotBeNull();
    }

    [Fact]
    public void Complete_ShouldRaiseStudySessionCompletedDomainEvent()
    {
        var session = StartNew();

        session.Complete(90);

        session.DomainEvents.Should().ContainSingle(e =>
            e is StudySessionCompletedDomainEvent);

        var evt = (StudySessionCompletedDomainEvent)session.DomainEvents[0];
        evt.StudySessionId.Should().Be(session.Id);
        evt.UserId.Should().Be(session.UserId);
        evt.Score.Should().Be(90);
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_ShouldBeIdempotent()
    {
        var session = StartNew();
        session.Complete(80);

        session.Complete(100); // second call — should be ignored

        session.Score.Should().Be(80, because: "second Complete() should be ignored");
        session.DomainEvents.Should().HaveCount(1, because: "event raised only once");
    }

    // ── Abandon ───────────────────────────────────────────────────────────

    [Fact]
    public void Abandon_ShouldSetStatusAbandoned()
    {
        var session = StartNew();

        session.Abandon();

        session.Status.Should().Be(SessionStatus.Abandoned);
        session.EndTime.Should().NotBeNull();
    }

    [Fact]
    public void Abandon_WhenAlreadyCompleted_ShouldBeIdempotent()
    {
        var session = StartNew();
        session.Complete(70);

        session.Abandon(); // should do nothing

        session.Status.Should().Be(SessionStatus.Completed,
            because: "Abandon() after Complete() should be ignored");
    }

    // ── Ping ─────────────────────────────────────────────────────────────

    [Fact]
    public void Ping_WhenInProgress_ShouldUpdateLastActivityTime()
    {
        var session = StartNew();
        var pingTime = DateTime.UtcNow.AddMinutes(5);

        session.Ping(pingTime);

        session.LastActivityTime.Should().Be(pingTime);
    }

    [Fact]
    public void Ping_WhenCompleted_ShouldNotUpdateLastActivityTime()
    {
        var session = StartNew();
        session.Complete(80);
        var activityBefore = session.LastActivityTime;

        session.Ping(DateTime.UtcNow.AddMinutes(5));

        session.LastActivityTime.Should().Be(activityBefore,
            because: "Ping() should be ignored for completed sessions");
    }
}
