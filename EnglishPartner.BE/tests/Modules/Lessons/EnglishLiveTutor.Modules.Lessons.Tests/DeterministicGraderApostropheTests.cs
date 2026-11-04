using EnglishLiveTutor.Domain.Common.Exercises;
using EnglishLiveTutor.Infrastructure.Lessons;

namespace EnglishLiveTutor.Domain.Tests;

public class DeterministicGraderApostropheTests
{
    private readonly DeterministicExerciseGrader _grader = new(new DictationScorer());

    private const string BlankContent = "{\"textWithBlanks\":\"She [blank-1] eat meat.\",\"blanks\":[{\"id\":\"blank-1\"}]}";
    private const string BlankGrading = "{\"isOpenEnded\":false,\"blankAnswers\":{\"blank-1\":{\"acceptedAnswers\":[\"doesn't\"],\"caseSensitive\":false}}}";

    [Theory]
    [InlineData("doesn’t")]
    [InlineData("doesn‘t")]
    [InlineData("doesn't")]
    public void FillInTheBlanks_ShouldAcceptCurlyApostrophes(string typed)
    {
        var answer = "{\"answers\":{\"blank-1\":\"" + typed + "\"}}";

        var result = _grader.Grade(ExerciseType.FillInTheBlanks, BlankContent, BlankGrading, answer, 10m);

        Assert.True(result.IsCorrect);
    }

    [Fact]
    public void ErrorCorrection_ShouldAcceptCurlyApostrophes()
    {
        const string content = "{\"incorrectSentence\":\"He doesn't goes to work.\"}";
        const string grading = "{\"referenceCorrections\":[\"He doesn't go to work.\"],\"targetErrors\":[\"goes\"]}";
        const string answer = "{\"correctedSentence\":\"He doesn’t go to work.\"}";

        var result = _grader.Grade(ExerciseType.ErrorCorrection, content, grading, answer, 10m);

        Assert.True(result.IsCorrect);
    }

    [Fact]
    public void FillInTheBlanks_ShouldStillRejectWrongWord()
    {
        const string answer = "{\"answers\":{\"blank-1\":\"don’t\"}}";

        var result = _grader.Grade(ExerciseType.FillInTheBlanks, BlankContent, BlankGrading, answer, 10m);

        Assert.False(result.IsCorrect);
    }
}
