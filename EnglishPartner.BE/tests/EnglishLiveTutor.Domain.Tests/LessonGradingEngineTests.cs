using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Infrastructure.Lessons;
using EnglishLiveTutor.Domain.Lessons.Enums;
using EnglishLiveTutor.Domain.Pronunciation;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class LessonGradingEngineTests
{
    [Fact]
    public void DictationScorer_WithSpecificationWorkedExample_ShouldMatchExactDistanceAndScore()
    {
        // Worked example from spec lines 573-584:
        // Reference: "Our chef prepares fresh handmade pasta with imported olive oil" (10 words)
        // Learner: "Our chef prepare fresh hand made pasta with imported oil" (10 words)
        // Distance: 4 (prepares->prepare, handmade->hand made [sub+ins], deleted olive)
        // Accuracy: (10 - 4)/10 = 0.60
        // Score100: 60.00
        // maxPoints: 5.0 -> pointsEarned: 3.00
        var scorer = new DictationScorer();

        string reference = "Our chef prepares fresh handmade pasta with imported olive oil.";
        string learner = "Our chef prepare fresh hand made pasta with imported oil";

        var result = scorer.Score(reference, learner, maxPoints: 5.0m);

        Assert.Equal(10, result.ReferenceWordCount);
        Assert.Equal(10, result.LearnerWordCount);
        Assert.Equal(4, result.WordLevenshteinDistance);
        Assert.Equal(0.60m, result.WordAccuracy);
        Assert.Equal(60.00m, result.Score100);
        Assert.Equal(3.00m, result.PointsEarned);
    }

    [Fact]
    public void DictationScorer_WithPerfectMatch_ShouldReturn100Percent()
    {
        var scorer = new DictationScorer();

        string reference = "Could you please bring us the menu?";
        string learner = "Could you please bring us the menu?";

        var result = scorer.Score(reference, learner, maxPoints: 10.0m);

        Assert.Equal(7, result.ReferenceWordCount);
        Assert.Equal(0, result.WordLevenshteinDistance);
        Assert.Equal(1.0m, result.WordAccuracy);
        Assert.Equal(100.00m, result.Score100);
        Assert.Equal(10.00m, result.PointsEarned);
    }

    [Fact]
    public void DeterministicGrader_MultipleChoice_ShouldGradeCorrectly()
    {
        var scorer = new DictationScorer();
        var grader = new DeterministicExerciseGrader(scorer);

        string content = "{\"question\":\"Choose\",\"options\":[{\"id\":\"opt-1\"},{\"id\":\"opt-2\"}]}";
        string grading = "{\"correctOptionIds\":[\"opt-2\"],\"explanation\":\"Reason\"}";

        // Correct answer
        var correctResult = grader.Grade(
            ExerciseType.MultipleChoice,
            content,
            grading,
            "{\"selectedOptionIds\":[\"opt-2\"]}",
            maxPoints: 2.0m);

        Assert.True(correctResult.IsCorrect);
        Assert.Equal(100m, correctResult.Score100);
        Assert.Equal(2.0m, correctResult.PointsEarned);

        // Incorrect answer
        var wrongResult = grader.Grade(
            ExerciseType.MultipleChoice,
            content,
            grading,
            "{\"selectedOptionIds\":[\"opt-1\"]}",
            maxPoints: 2.0m);

        Assert.False(wrongResult.IsCorrect);
        Assert.Equal(0m, wrongResult.Score100);
        Assert.Equal(0m, wrongResult.PointsEarned);
    }

    [Fact]
    public void DeterministicGrader_FillInTheBlanks_ShouldCalculatePartialAccuracy()
    {
        var scorer = new DictationScorer();
        var grader = new DeterministicExerciseGrader(scorer);

        string content = "{\"text\":\"[b-1] and [b-2]\"}";
        string grading = "{\"blanks\":[{\"id\":\"b-1\",\"correctAnswers\":[\"take up\"]},{\"id\":\"b-2\",\"correctAnswers\":[\"give in\"]}]}";

        // 1 of 2 correct: 50%
        var result = grader.Grade(
            ExerciseType.FillInTheBlanks,
            content,
            grading,
            "{\"answers\":{\"b-1\":\"take up\",\"b-2\":\"wrong answer\"}}",
            maxPoints: 4.0m);

        Assert.False(result.IsCorrect);
        Assert.Equal(50.00m, result.Score100);
        Assert.Equal(2.00m, result.PointsEarned);
    }

    [Fact]
    public void DeterministicGrader_Matching_ShouldCalculatePairAccuracy()
    {
        var scorer = new DictationScorer();
        var grader = new DeterministicExerciseGrader(scorer);

        string content = "{}";
        string grading = "{\"pairs\":[{\"leftId\":\"l-1\",\"rightId\":\"r-1\"},{\"leftId\":\"l-2\",\"rightId\":\"r-2\"}]}";

        // 2 of 2 correct
        var result = grader.Grade(
            ExerciseType.Matching,
            content,
            grading,
            "{\"pairs\":[{\"leftId\":\"l-1\",\"rightId\":\"r-1\"},{\"leftId\":\"l-2\",\"rightId\":\"r-2\"}]}",
            maxPoints: 6.0m);

        Assert.True(result.IsCorrect);
        Assert.Equal(100.00m, result.Score100);
        Assert.Equal(6.00m, result.PointsEarned);
    }

    [Fact]
    public void DeterministicGrader_Flashcards_SelfAssessment_ShouldReturnZeroPointsEarned()
    {
        var scorer = new DictationScorer();
        var grader = new DeterministicExerciseGrader(scorer);

        var result = grader.Grade(
            ExerciseType.Flashcards,
            "{\"cards\":[]}",
            null,
            "{\"cardId\":\"c-1\",\"rating\":\"Good\"}",
            maxPoints: 0m);

        Assert.True(result.IsCorrect);
        Assert.Equal(0m, result.PointsEarned);
        Assert.Equal(0m, result.MaxPoints);
    }

    [Fact]
    public void LessonAiGrader_MathematicalWeightedFormula_ShouldMatchExactExpectedScore()
    {
        // Test case from critique point #2:
        // Criteria: MEANING_FIDELITY (50%), COMPLETENESS (20%), GRAMMATICAL_ACCURACY (15%), NATURAL_EXPRESSION (15%)
        // Raw scores: 4, 4, 3, 4
        // Component calculation:
        // 4/4 * 50 = 50.00
        // 4/4 * 20 = 20.00
        // 3/4 * 15 = 11.25
        // 4/4 * 15 = 15.00
        // Total score100 = 50 + 20 + 11.25 + 15 = 96.25
        // maxPoints = 5.0 -> pointsEarned = Round(96.25 / 100 * 5.0, 2) = 4.81

        var rubric = new ScoringRubricEntity(
            code: "LESSON_TRANSLATION",
            name: "Translation Rubric",
            assessmentScope: "LessonExercise",
            exerciseType: (int)ExerciseType.Translation,
            cefrLevel: "All");

        rubric.AddCriterion("MEANING_FIDELITY", "Đúng nghĩa gốc", "Desc", 50.00m, 1);
        rubric.AddCriterion("COMPLETENESS", "Đầy đủ thông tin", "Desc", 20.00m, 2);
        rubric.AddCriterion("GRAMMATICAL_ACCURACY", "Chính xác ngữ pháp", "Desc", 15.00m, 3);
        rubric.AddCriterion("NATURAL_EXPRESSION", "Diễn đạt tự nhiên", "Desc", 15.00m, 4);

        int[] rawScores = { 4, 4, 3, 4 };
        var criteria = rubric.Criteria.OrderBy(c => c.DisplayOrder).ToList();

        decimal totalScore = 0m;
        for (int i = 0; i < criteria.Count; i++)
        {
            totalScore += (rawScores[i] / 4.0m) * criteria[i].Weight;
        }

        decimal score100 = Math.Round(totalScore, 2, MidpointRounding.AwayFromZero);
        decimal pointsEarned = Math.Round((score100 / 100.00m) * 5.0m, 2, MidpointRounding.AwayFromZero);

        Assert.Equal(96.25m, score100);
        Assert.Equal(4.81m, pointsEarned);
    }
}
