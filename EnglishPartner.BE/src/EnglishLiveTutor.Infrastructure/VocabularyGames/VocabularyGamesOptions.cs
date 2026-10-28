namespace EnglishLiveTutor.Infrastructure.VocabularyGames;

public sealed class VocabularyGamesOptions
{
    public const string SectionName = "VocabularyGames";
    public const int DefaultExpirationPollSeconds = 10;
    public const int MinExpirationPollSeconds = 1;
    public const int MaxExpirationPollSeconds = 300;

    public int ExpirationPollSeconds { get; set; } = DefaultExpirationPollSeconds;
}
