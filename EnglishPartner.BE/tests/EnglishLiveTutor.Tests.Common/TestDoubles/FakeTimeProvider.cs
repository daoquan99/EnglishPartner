namespace EnglishLiveTutor.Domain.Tests;

public sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FakeTimeProvider(DateTimeOffset initialUtcNow)
    {
        _utcNow = initialUtcNow;
    }

    public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);

    public override DateTimeOffset GetUtcNow() => _utcNow;
}
