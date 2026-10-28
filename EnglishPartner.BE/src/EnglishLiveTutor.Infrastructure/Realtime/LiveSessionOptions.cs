namespace EnglishLiveTutor.Infrastructure.Realtime;

public sealed class LiveSessionOptions
{
    public const string SectionName = "LiveSession";

    public int IdleTimeoutMinutes { get; set; } = 15;
}
