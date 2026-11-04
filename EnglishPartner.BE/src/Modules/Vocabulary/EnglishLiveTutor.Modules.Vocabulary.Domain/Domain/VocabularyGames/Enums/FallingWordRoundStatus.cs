namespace EnglishLiveTutor.Domain.VocabularyGames.Enums;

public enum FallingWordRoundStatus
{
    Queued = 1,
    Active = 2,
    Correct = 3,
    Incorrect = 4,
    TimedOut = 5,
    Abandoned = 6,
}
