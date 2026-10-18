namespace EnglishLiveTutor.Domain.Lessons.Enums;

/// <summary>
/// The 16 canonical exercise types supported across the system.
/// Values 1..16 are mapped to database integers and PascalCase strings on the wire.
/// </summary>
public enum ExerciseType
{
    Flashcards = 1,
    SpeakingPractice = 2,
    MultipleChoice = 3,
    FillInTheBlanks = 4,
    Matching = 5,
    SpellingPractice = 6,
    SentenceUnscramble = 7,
    ErrorCorrection = 8,
    Translation = 9,
    TrueFalse = 10,
    ReadingComprehension = 11,
    ListeningComprehension = 12,
    Dictation = 13,
    Shadowing = 14,
    ConversationRolePlay = 15,
    WritingPractice = 16
}
