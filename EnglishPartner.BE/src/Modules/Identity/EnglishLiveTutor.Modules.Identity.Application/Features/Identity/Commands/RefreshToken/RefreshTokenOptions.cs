namespace EnglishLiveTutor.Application.Features.Identity.Commands.RefreshToken;

public sealed class RefreshTokenOptions
{
    public const int DefaultGraceSeconds = 10;

    public int GraceSeconds { get; set; }

    public RefreshTokenOptions(int graceSeconds = DefaultGraceSeconds)
    {
        GraceSeconds = graceSeconds >= 0 ? graceSeconds : 0;
    }
}
