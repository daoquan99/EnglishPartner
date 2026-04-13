using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Curriculum.Domain.Exercises;

/// <summary>
/// An exercise that tests the user's knowledge.
/// Types: MultipleChoice, FillInTheBlank, TrueFalse, Matching, Translation, Listening, Speaking.
/// </summary>
public sealed class Exercise : AggregateRoot
{
    public string Title { get; private set; } = null!;
    public string Instruction { get; private set; } = null!;
    public ExerciseType Type { get; private set; }
    public string LanguageCode { get; private set; } = "en";
    public string? Level { get; private set; }
    public Guid? TopicId { get; private set; }
    public Guid? VocabularyId { get; private set; }
    public Guid? GrammarId { get; private set; }
    public string? Content { get; private set; }
    public string CorrectAnswer { get; private set; } = null!;
    public string? Explanation { get; private set; }
    public int XpReward { get; private set; } = 10;

    private readonly List<Entities.ExerciseOption> _options = [];
    public IReadOnlyList<Entities.ExerciseOption> Options => _options.AsReadOnly();

    private Exercise() { }

    public static Exercise Create(
        string title,
        string instruction,
        ExerciseType type,
        string correctAnswer,
        string languageCode,
        string? level = null,
        Guid? topicId = null,
        Guid? vocabularyId = null,
        Guid? grammarId = null,
        string? content = null,
        string? explanation = null,
        int xpReward = 10)
    {
        return new Exercise
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Instruction = instruction.Trim(),
            Type = type,
            CorrectAnswer = correctAnswer.Trim(),
            LanguageCode = languageCode.ToLowerInvariant(),
            Level = level,
            TopicId = topicId,
            VocabularyId = vocabularyId,
            GrammarId = grammarId,
            Content = content,
            Explanation = explanation,
            XpReward = xpReward
        };
    }

    public void Update(
        string title,
        string instruction,
        ExerciseType type,
        string correctAnswer,
        string? level,
        Guid? topicId,
        Guid? vocabularyId,
        Guid? grammarId,
        string? content,
        string? explanation,
        int xpReward)
    {
        Title = title.Trim();
        Instruction = instruction.Trim();
        Type = type;
        CorrectAnswer = correctAnswer.Trim();
        Level = level;
        TopicId = topicId;
        VocabularyId = vocabularyId;
        GrammarId = grammarId;
        Content = content;
        Explanation = explanation;
        XpReward = xpReward;
    }

    public void AddOption(string text, bool isCorrect)
    {
        _options.Add(Entities.ExerciseOption.Create(Id, text, isCorrect, _options.Count));
    }

    public void ClearOptions() => _options.Clear();
}

public enum ExerciseType
{
    MultipleChoice = 0,
    FillInTheBlank = 1,
    TrueFalse = 2,
    Matching = 3,
    Translation = 4,
    ListeningComprehension = 5,
    Speaking = 6,
    SentenceTransformation = 7,
    ErrorIdentification = 8,
    SentenceCompletion = 9,
    WordForm = 10,
    VerbTense = 11,
    ReadingComprehension = 12,
    Writing = 13,
    TrueFalseNotGiven = 14,
    YesNoQuestions = 15,
    WhQuestions = 16,
    TagQuestions = 17,
    Paraphrasing = 18,
    GapFilling = 19,
    ClozeTest = 20,
    Dictation = 21,
    ReorderingWords = 22,
    RewritingSentences = 23,
    DialogueCompletion = 24,
    PictureDescription = 25,
    EssayWriting = 26,
    ShortAnswerQuestions = 27
}
