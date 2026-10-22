using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Lessons.Enums;
using EnglishLiveTutor.Infrastructure.Lessons;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class LessonPlayerAllExerciseTypesTests
{
    private readonly DeterministicExerciseGrader _grader = new(new DictationScorer());

    [Fact]
    public void Matching_WithRuntimeFixture_LearnerPermutationScoresPartial_Not100Percent()
    {
        // Fixture from runtime DB (BCC0476F-EA4C-417F-BAF1-FE04BD8F5C12):
        // correctPairs: l1-r2, l2-r5, l3-r3, l4-r4, l5-r1
        // Learner submitted: l1-r1, l2-r2, l3-r3, l4-r4, l5-r5 -> Only l3-r3 matches!
        string grading = """
            {
              "correctPairs": [
                {"leftId":"l1","rightId":"r2"},
                {"leftId":"l2","rightId":"r5"},
                {"leftId":"l3","rightId":"r3"},
                {"leftId":"l4","rightId":"r4"},
                {"leftId":"l5","rightId":"r1"}
              ]
            }
            """;

        string learnerAnswer = """
            {
              "pairs": [
                {"leftId":"l1","rightId":"r1"},
                {"leftId":"l2","rightId":"r2"},
                {"leftId":"l3","rightId":"r3"},
                {"leftId":"l4","rightId":"r4"},
                {"leftId":"l5","rightId":"r5"}
              ]
            }
            """;

        var result = _grader.Grade(ExerciseType.Matching, "{}", grading, learnerAnswer, maxPoints: 10m);

        Assert.False(result.IsCorrect);
        Assert.Equal(40.00m, result.Score100);
        Assert.Equal(4.00m, result.PointsEarned);
    }

    [Fact]
    public void Matching_ExactMatches_AwardsFullPoints()
    {
        string grading = """
            {
              "correctPairs": [
                {"leftId":"l1","rightId":"r2"},
                {"leftId":"l2","rightId":"r5"}
              ]
            }
            """;

        string learnerAnswer = """
            {
              "pairs": [
                {"leftId":"l1","rightId":"r2"},
                {"leftId":"l2","rightId":"r5"}
              ]
            }
            """;

        var result = _grader.Grade(ExerciseType.Matching, "{}", grading, learnerAnswer, maxPoints: 10m);

        Assert.True(result.IsCorrect);
        Assert.Equal(100.00m, result.Score100);
        Assert.Equal(10.00m, result.PointsEarned);
    }

    [Fact]
    public void Matching_DuplicateSubmittedPairs_DoNotInflateScore()
    {
        string grading = """
            {
              "correctPairs": [
                {"leftId":"l1","rightId":"r2"},
                {"leftId":"l2","rightId":"r5"}
              ]
            }
            """;

        string learnerAnswer = """
            {
              "pairs": [
                {"leftId":"l1","rightId":"r2"},
                {"leftId":"l1","rightId":"r2"}
              ]
            }
            """;

        var result = _grader.Grade(ExerciseType.Matching, "{}", grading, learnerAnswer, maxPoints: 10m);

        Assert.False(result.IsCorrect);
        Assert.Equal(50.00m, result.Score100);
        Assert.Equal(5.00m, result.PointsEarned);
    }

    [Fact]
    public void Matching_MissingDefinition_ThrowsConfigurationError_InsteadOf100Percent()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _grader.Grade(ExerciseType.Matching, "{}", "{}", "{\"pairs\":[]}", maxPoints: 10m));
    }

    [Fact]
    public void Spelling_WithRuntimeFixture_ScheduleCaseInsensitive_Scores100Percent()
    {
        // Fixture from runtime DB (9CAAFCE1-D9FF-4445-A9AC-201363578485):
        // correctAnswer: schedule, acceptedAnswers: [schedule], caseSensitive: false
        string grading = """
            {
              "correctAnswer": "schedule",
              "acceptedAnswers": ["schedule"],
              "caseSensitive": false
            }
            """;

        var resultCapitalized = _grader.Grade(ExerciseType.SpellingPractice, "{}", grading, "{\"spelledWord\":\"Schedule\"}", maxPoints: 10m);
        Assert.True(resultCapitalized.IsCorrect);
        Assert.Equal(100.00m, resultCapitalized.Score100);
        Assert.Equal(10.00m, resultCapitalized.PointsEarned);

        var resultTrimmed = _grader.Grade(ExerciseType.SpellingPractice, "{}", grading, "{\"spelledWord\":\"  schedule  \"}", maxPoints: 10m);
        Assert.True(resultTrimmed.IsCorrect);
        Assert.Equal(100.00m, resultTrimmed.Score100);

        var resultTypo = _grader.Grade(ExerciseType.SpellingPractice, "{}", grading, "{\"spelledWord\":\"Scheduel\"}", maxPoints: 10m);
        Assert.False(resultTypo.IsCorrect);
        Assert.Equal(0.00m, resultTypo.Score100);
        Assert.Equal(0.00m, resultTypo.PointsEarned);
    }

    [Fact]
    public void Spelling_CaseSensitiveTrue_RejectsMismatchedCasing()
    {
        string grading = """
            {
              "targetWord": "schedule",
              "acceptedSpellings": ["schedule"],
              "caseSensitive": true
            }
            """;

        var result = _grader.Grade(ExerciseType.SpellingPractice, "{}", grading, "{\"spelledWord\":\"Schedule\"}", maxPoints: 10m);
        Assert.False(result.IsCorrect);
        Assert.Equal(0.00m, result.Score100);
    }

    [Fact]
    public void Spelling_MissingTarget_ThrowsConfigurationError()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _grader.Grade(ExerciseType.SpellingPractice, "{}", "{}", "{\"spelledWord\":\"schedule\"}", maxPoints: 10m));
    }

    [Fact]
    public void FillInTheBlanks_WithRuntimeFixture_ScoresAccuratelyWithoutThrowing()
    {
        // Fixture from runtime DB (F5D932E0-6877-42E5-AFCE-0A1B4C845F39):
        string grading = """
            {
              "correctAnswers": [
                {"blankId":"b1","acceptedAnswers":["wake up"]},
                {"blankId":"b2","acceptedAnswers":["get ready"]},
                {"blankId":"b3","acceptedAnswers":["daily routine"]}
              ],
              "caseSensitive": false,
              "trimWhitespace": true
            }
            """;

        // All incorrect:
        var resultWrong = _grader.Grade(ExerciseType.FillInTheBlanks, "{}", grading, "{\"answers\":{\"b1\":\"x\",\"b2\":\"x\",\"b3\":\"x\"}}", maxPoints: 10m);
        Assert.False(resultWrong.IsCorrect);
        Assert.Equal(0.00m, resultWrong.Score100);

        // All correct:
        var resultCorrect = _grader.Grade(ExerciseType.FillInTheBlanks, "{}", grading, "{\"answers\":{\"b1\":\"wake up\",\"b2\":\"get ready\",\"b3\":\"daily routine\"}}", maxPoints: 10m);
        Assert.True(resultCorrect.IsCorrect);
        Assert.Equal(100.00m, resultCorrect.Score100);
        Assert.Equal(10.00m, resultCorrect.PointsEarned);

        // 1 of 3 correct:
        var resultPartial = _grader.Grade(ExerciseType.FillInTheBlanks, "{}", grading, "{\"answers\":{\"b1\":\"wake up\",\"b2\":\"wrong\",\"b3\":\"wrong\"}}", maxPoints: 10m);
        Assert.False(resultPartial.IsCorrect);
        Assert.Equal(33.33m, resultPartial.Score100);
        Assert.Equal(3.33m, resultPartial.PointsEarned);
    }

    [Fact]
    public void TrueFalse_MissingDefinition_ThrowsConfigurationError_InsteadOf100Percent()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _grader.Grade(ExerciseType.TrueFalse, "{}", "{}", "{\"answers\":{\"s1\":true}}", maxPoints: 10m));
    }

    [Fact]
    public void TrueFalse_CanonicalAndLegacyAnswers_GradesAccurately()
    {
        string grading = """
            {
              "statementAnswers": {
                "s1": true,
                "s2": false
              }
            }
            """;

        var result = _grader.Grade(ExerciseType.TrueFalse, "{}", grading, "{\"answers\":{\"s1\":true,\"s2\":true}}", maxPoints: 10m);
        Assert.False(result.IsCorrect);
        Assert.Equal(50.00m, result.Score100);
        Assert.Equal(5.00m, result.PointsEarned);
    }

    [Fact]
    public void MultipleChoice_EmptyCorrectOptionIds_ThrowsConfigurationError_PreventingEmptySetSuccess()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _grader.Grade(ExerciseType.MultipleChoice, "{}", "{\"correctOptionIds\":[]}", "{\"selectedOptionIds\":[]}", maxPoints: 10m));
    }

    [Fact]
    public void MultipleChoice_ExactSetRequired_ForFullCredit()
    {
        string grading = """
            {
              "correctOptionIds": ["opt_a", "opt_b"]
            }
            """;

        // Partial selection -> 0 points
        var partial = _grader.Grade(ExerciseType.MultipleChoice, "{}", grading, "{\"selectedOptionIds\":[\"opt_a\"]}", maxPoints: 10m);
        Assert.False(partial.IsCorrect);
        Assert.Equal(0m, partial.Score100);

        // Extra selection -> 0 points
        var extra = _grader.Grade(ExerciseType.MultipleChoice, "{}", grading, "{\"selectedOptionIds\":[\"opt_a\",\"opt_b\",\"opt_c\"]}", maxPoints: 10m);
        Assert.False(extra.IsCorrect);
        Assert.Equal(0m, extra.Score100);

        // Exact match -> 100%
        var exact = _grader.Grade(ExerciseType.MultipleChoice, "{}", grading, "{\"selectedOptionIds\":[\"opt_b\",\"opt_a\"]}", maxPoints: 10m);
        Assert.True(exact.IsCorrect);
        Assert.Equal(100.00m, exact.Score100);
        Assert.Equal(10.00m, exact.PointsEarned);
    }

    [Fact]
    public void SentenceUnscramble_MissingDefinition_ThrowsConfigurationError()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _grader.Grade(ExerciseType.SentenceUnscramble, "{}", "{}", "{\"orderedTokenIds\":[\"t1\"]}", maxPoints: 10m));
    }

    [Fact]
    public void ErrorCorrection_MissingDefinition_ThrowsConfigurationError()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _grader.Grade(ExerciseType.ErrorCorrection, "{}", "{}", "{\"correctedSentence\":\"Test\"}", maxPoints: 10m));
    }
}
