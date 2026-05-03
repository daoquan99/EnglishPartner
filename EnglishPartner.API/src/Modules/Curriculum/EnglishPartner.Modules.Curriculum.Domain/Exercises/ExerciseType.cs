namespace EnglishPartner.Modules.Curriculum.Domain.Exercises;

public enum ExerciseType
{
    // ── Core types ───────────────────────────────────────────────────────
    MultipleChoice = 0,
    FillInTheBlank = 1,
    TrueFalse = 2,
    Matching = 3,
    Translation = 4,
    SentenceCompletion = 5,
    WordForm = 6,
    VerbTense = 7,
    TrueFalseNotGiven = 8,
    Paraphrasing = 9,
    ReorderingWords = 10,
    RewritingSentences = 11,
    DialogueCompletion = 12,
    ShortAnswerQuestions = 13,
    QuestionFormation = 14,

    // ── Vocabulary-focused ───────────────────────────────────────────────
    SynonymAntonym = 15,
    Collocation = 16,
    OddOneOut = 17,
    SpellingCorrection = 18,
    WordStress = 19,

    // ── Advanced / multi-answer ──────────────────────────────────────────
    MultipleResponse = 20,
    SentenceCorrection = 21,
    Categorization = 22,

    // ── Media-based (không hỗ trợ AI generate) ──────────────────────────
    ListeningComprehension = 23,
    ListeningGapFill = 24,
    Speaking = 25,
    ReadingComprehension = 26,
    Writing = 27,
    Dictation = 28,
    PictureDescription = 29,
    TopicResponse = 30,
}

public static class ExerciseTypeExtensions
{
    /// <summary>
    /// Returns the default number of answer options for a given exercise type.
    /// 0 means the type does not use selectable options (free-form answer).
    /// </summary>
    public static int GetDefaultOptionCount(this ExerciseType type) => type switch
    {
        ExerciseType.MultipleChoice      => 4,
        ExerciseType.FillInTheBlank      => 4,
        ExerciseType.TrueFalse           => 2,
        ExerciseType.SentenceCompletion  => 4,
        ExerciseType.WordForm            => 4,
        ExerciseType.VerbTense           => 4,
        ExerciseType.TrueFalseNotGiven   => 3,
        ExerciseType.DialogueCompletion  => 4,
        ExerciseType.QuestionFormation   => 4,
        ExerciseType.SynonymAntonym      => 4,
        ExerciseType.Collocation         => 4,
        ExerciseType.OddOneOut           => 4,
        ExerciseType.WordStress          => 4,
        ExerciseType.MultipleResponse    => 5, // ≥2 correct
        _ => 0
    };

    /// <summary>True when this exercise type must have selectable answer options.</summary>
    public static bool RequiresOptions(this ExerciseType type) => type.GetDefaultOptionCount() > 0;
}
