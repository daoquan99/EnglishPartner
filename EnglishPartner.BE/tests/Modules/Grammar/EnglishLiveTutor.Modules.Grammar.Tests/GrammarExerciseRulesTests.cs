using EnglishLiveTutor.Application.Features.Grammar.Rules;
using EnglishLiveTutor.Domain.Common.Exercises;
using EnglishLiveTutor.Infrastructure.Lessons;

namespace EnglishLiveTutor.Domain.Tests;

public class GrammarExerciseRulesTests
{
    private readonly DeterministicExerciseGrader _grader = new(new DictationScorer());

    [Fact]
    public void AllowedTypes_ContainsExactNineTypes()
    {
        Assert.Equal(9, GrammarExerciseRules.AllowedTypes.Count);
        Assert.Contains(ExerciseType.TrueFalse, GrammarExerciseRules.AllowedTypes);
        Assert.Contains(ExerciseType.MultipleChoice, GrammarExerciseRules.AllowedTypes);
        Assert.Contains(ExerciseType.Matching, GrammarExerciseRules.AllowedTypes);
        Assert.Contains(ExerciseType.FillInTheBlanks, GrammarExerciseRules.AllowedTypes);
        Assert.Contains(ExerciseType.SentenceUnscramble, GrammarExerciseRules.AllowedTypes);
        Assert.Contains(ExerciseType.ErrorCorrection, GrammarExerciseRules.AllowedTypes);
        Assert.Contains(ExerciseType.Translation, GrammarExerciseRules.AllowedTypes);
        Assert.Contains(ExerciseType.WritingPractice, GrammarExerciseRules.AllowedTypes);
        Assert.Contains(ExerciseType.ReadingComprehension, GrammarExerciseRules.AllowedTypes);
    }

    [Theory]
    [InlineData(ExerciseType.Flashcards)]
    [InlineData(ExerciseType.Dictation)]
    [InlineData(ExerciseType.SpeakingPractice)]
    [InlineData(ExerciseType.Shadowing)]
    [InlineData(ExerciseType.SpellingPractice)]
    [InlineData(ExerciseType.ConversationRolePlay)]
    [InlineData(ExerciseType.ListeningComprehension)]
    public void Validate_DisallowedTypes_ReturnsError(ExerciseType type)
    {
        var issues = GrammarExerciseRules.Validate(type, "{}", null, 10m, GradingMode.Deterministic);
        Assert.Contains(issues, i => i.Severity == "Error" && i.Message.Contains("not supported"));
    }

    [Fact]
    public void DeterministicTypes_MissingGrading_ReturnsError()
    {
        const string mcContent = "{\"options\":[{\"id\":\"o1\",\"text\":\"A\"},{\"id\":\"o2\",\"text\":\"B\"}],\"selectionMode\":\"Single\"}";
        var issues = GrammarExerciseRules.Validate(ExerciseType.MultipleChoice, mcContent, null, 10m, GradingMode.Deterministic);
        Assert.Contains(issues, i => i.Severity == "Error" && i.Path == "grading");
    }

    [Fact]
    public void MultipleChoice_ValidAndMissingAnswer()
    {
        const string validContent = "{\"options\":[{\"id\":\"o1\",\"text\":\"A\"},{\"id\":\"o2\",\"text\":\"B\"}],\"selectionMode\":\"Single\"}";
        const string validGrading = "{\"correctOptionIds\":[\"o1\"]}";
        const string missingGrading = "{\"correctOptionIds\":[]}";
        const string singleWithMultipleAnswers = "{\"correctOptionIds\":[\"o1\",\"o2\"]}";
        const string unknownOptionGrading = "{\"correctOptionIds\":[\"o999\"]}";

        var valid = GrammarExerciseRules.Validate(ExerciseType.MultipleChoice, validContent, validGrading, 10m, GradingMode.Deterministic);
        Assert.DoesNotContain(valid, i => i.Severity == "Error");

        var invalidMissing = GrammarExerciseRules.Validate(ExerciseType.MultipleChoice, validContent, missingGrading, 10m, GradingMode.Deterministic);
        Assert.Contains(invalidMissing, i => i.Severity == "Error");

        var invalidMultiple = GrammarExerciseRules.Validate(ExerciseType.MultipleChoice, validContent, singleWithMultipleAnswers, 10m, GradingMode.Deterministic);
        Assert.Contains(invalidMultiple, i => i.Severity == "Error");

        var invalidUnknown = GrammarExerciseRules.Validate(ExerciseType.MultipleChoice, validContent, unknownOptionGrading, 10m, GradingMode.Deterministic);
        Assert.Contains(invalidUnknown, i => i.Severity == "Error");
    }

    [Fact]
    public void TrueFalse_ValidAndMissingAnswer()
    {
        const string validContent = "{\"statements\":[{\"id\":\"s1\",\"text\":\"He is tall.\"},{\"id\":\"s2\",\"text\":\"She are doctor.\"}]}";
        const string validGrading = "{\"statementAnswers\":{\"s1\":true,\"s2\":false}}";
        const string missingStatementGrading = "{\"statementAnswers\":{\"s1\":true}}"; // misses s2
        const string nonBooleanGrading = "{\"statementAnswers\":{\"s1\":\"yes\",\"s2\":false}}";

        var valid = GrammarExerciseRules.Validate(ExerciseType.TrueFalse, validContent, validGrading, 10m, GradingMode.Deterministic);
        Assert.DoesNotContain(valid, i => i.Severity == "Error");

        var invalidMissing = GrammarExerciseRules.Validate(ExerciseType.TrueFalse, validContent, missingStatementGrading, 10m, GradingMode.Deterministic);
        Assert.Contains(invalidMissing, i => i.Severity == "Error");

        var invalidNonBool = GrammarExerciseRules.Validate(ExerciseType.TrueFalse, validContent, nonBooleanGrading, 10m, GradingMode.Deterministic);
        Assert.Contains(invalidNonBool, i => i.Severity == "Error");
    }

    [Fact]
    public void Matching_ValidAndMissingAnswer()
    {
        const string validContent = "{\"leftItems\":[{\"id\":\"l1\",\"text\":\"L1\"},{\"id\":\"l2\",\"text\":\"L2\"}],\"rightItems\":[{\"id\":\"r1\",\"text\":\"R1\"},{\"id\":\"r2\",\"text\":\"R2\"}]}";
        const string validGrading = "{\"correctPairs\":[{\"leftId\":\"l1\",\"rightId\":\"r1\"},{\"leftId\":\"l2\",\"rightId\":\"r2\"}]}";
        const string missingGrading = "{\"correctPairs\":[]}";
        const string duplicateLeftGrading = "{\"correctPairs\":[{\"leftId\":\"l1\",\"rightId\":\"r1\"},{\"leftId\":\"l1\",\"rightId\":\"r2\"}]}";
        const string invalidIdGrading = "{\"correctPairs\":[{\"leftId\":\"unknown\",\"rightId\":\"r1\"}]}";

        var valid = GrammarExerciseRules.Validate(ExerciseType.Matching, validContent, validGrading, 10m, GradingMode.Deterministic);
        Assert.DoesNotContain(valid, i => i.Severity == "Error");

        var invalidMissing = GrammarExerciseRules.Validate(ExerciseType.Matching, validContent, missingGrading, 10m, GradingMode.Deterministic);
        Assert.Contains(invalidMissing, i => i.Severity == "Error");

        var invalidDupLeft = GrammarExerciseRules.Validate(ExerciseType.Matching, validContent, duplicateLeftGrading, 10m, GradingMode.Deterministic);
        Assert.Contains(invalidDupLeft, i => i.Severity == "Error");

        var invalidId = GrammarExerciseRules.Validate(ExerciseType.Matching, validContent, invalidIdGrading, 10m, GradingMode.Deterministic);
        Assert.Contains(invalidId, i => i.Severity == "Error");
    }

    [Fact]
    public void FillInTheBlanks_ValidAndMissingAnswer()
    {
        const string validContent = "{\"textWithBlanks\":\"I am a [blank-1] and she is a [blank-2].\",\"blanks\":[{\"id\":\"blank-1\"},{\"id\":\"blank-2\"}]}";
        const string validGrading = "{\"blankAnswers\":{\"blank-1\":{\"acceptedAnswers\":[\"teacher\"]},\"blank-2\":{\"acceptedAnswers\":[\"doctor\"]}}}";

        var valid = GrammarExerciseRules.Validate(ExerciseType.FillInTheBlanks, validContent, validGrading, 10m, GradingMode.Deterministic);
        Assert.DoesNotContain(valid, i => i.Severity == "Error");

        // Missing grading definition entirely
        var invalidNoGrading = GrammarExerciseRules.Validate(ExerciseType.FillInTheBlanks, validContent, null, 10m, GradingMode.Deterministic);
        Assert.Contains(invalidNoGrading, i => i.Severity == "Error");

        // Marker in text without blank definition
        const string extraMarkerContent = "{\"textWithBlanks\":\"I am a [blank-1] and [blank-extra].\",\"blanks\":[{\"id\":\"blank-1\"}]}";
        var invalidExtraMarker = GrammarExerciseRules.Validate(ExerciseType.FillInTheBlanks, extraMarkerContent, validGrading, 10m, GradingMode.Deterministic);
        Assert.Contains(invalidExtraMarker, i => i.Severity == "Error");

        // Blank definition without marker in text
        const string missingMarkerContent = "{\"textWithBlanks\":\"I am a [blank-1].\",\"blanks\":[{\"id\":\"blank-1\"},{\"id\":\"blank-2\"}]}";
        var invalidMissingMarker = GrammarExerciseRules.Validate(ExerciseType.FillInTheBlanks, missingMarkerContent, validGrading, 10m, GradingMode.Deterministic);
        Assert.Contains(invalidMissingMarker, i => i.Severity == "Error");

        // Blank missing accepted answers
        const string missingAnswerGrading = "{\"blankAnswers\":{\"blank-1\":{\"acceptedAnswers\":[\"teacher\"]},\"blank-2\":{\"acceptedAnswers\":[]}}}";
        var invalidBlankMissingAnswer = GrammarExerciseRules.Validate(ExerciseType.FillInTheBlanks, validContent, missingAnswerGrading, 10m, GradingMode.Deterministic);
        Assert.Contains(invalidBlankMissingAnswer, i => i.Severity == "Error");
    }

    [Fact]
    public void SentenceUnscramble_ValidAndMissingAnswer()
    {
        const string validContent = "{\"tokens\":[{\"id\":\"t1\",\"text\":\"He\"},{\"id\":\"t2\",\"text\":\"runs\"}]}";
        const string validGrading = "{\"validSequences\":[[\"t1\",\"t2\"]]}";
        const string unknownTokenGrading = "{\"validSequences\":[[\"unknown\",\"t2\"]]}";
        const string missingTokenGrading = "{\"validSequences\":[[\"t1\"]]}"; // doesn't use t2
        const string duplicateTokenGrading = "{\"validSequences\":[[\"t1\",\"t1\"]]}";

        var valid = GrammarExerciseRules.Validate(ExerciseType.SentenceUnscramble, validContent, validGrading, 10m, GradingMode.Deterministic);
        Assert.DoesNotContain(valid, i => i.Severity == "Error");

        var invalidUnknown = GrammarExerciseRules.Validate(ExerciseType.SentenceUnscramble, validContent, unknownTokenGrading, 10m, GradingMode.Deterministic);
        Assert.Contains(invalidUnknown, i => i.Severity == "Error");

        var invalidMissingToken = GrammarExerciseRules.Validate(ExerciseType.SentenceUnscramble, validContent, missingTokenGrading, 10m, GradingMode.Deterministic);
        Assert.Contains(invalidMissingToken, i => i.Severity == "Error");

        var invalidDupToken = GrammarExerciseRules.Validate(ExerciseType.SentenceUnscramble, validContent, duplicateTokenGrading, 10m, GradingMode.Deterministic);
        Assert.Contains(invalidDupToken, i => i.Severity == "Error");
    }

    [Fact]
    public void ErrorCorrection_ValidAndMissingAnswer()
    {
        const string content = "{\"incorrectSentence\":\"She have a cat.\"}";
        const string validGrading = "{\"referenceCorrections\":[\"She has a cat.\"]}";
        const string missingGrading = "{\"referenceCorrections\":[]}";
        const string identicalCorrectionGrading = "{\"referenceCorrections\":[\"  she have a cat. \"]}"; // identical ignoring case/whitespace

        var valid = GrammarExerciseRules.Validate(ExerciseType.ErrorCorrection, content, validGrading, 10m, GradingMode.Deterministic);
        Assert.DoesNotContain(valid, i => i.Severity == "Error");

        var invalidMissing = GrammarExerciseRules.Validate(ExerciseType.ErrorCorrection, content, missingGrading, 10m, GradingMode.Deterministic);
        Assert.Contains(invalidMissing, i => i.Severity == "Error");

        var invalidIdentical = GrammarExerciseRules.Validate(ExerciseType.ErrorCorrection, content, identicalCorrectionGrading, 10m, GradingMode.Deterministic);
        Assert.Contains(invalidIdentical, i => i.Severity == "Error");
    }

    [Fact]
    public void Translation_ValidAndMissingAnswer()
    {
        const string validContent = "{\"sourceText\":\"Cô ấy là bác sĩ.\"}";
        const string validGrading = "{\"rubricCode\":\"LESSON_TRANSLATION\",\"referenceTranslations\":[\"She is a doctor.\"]}";
        const string missingGrading = "{\"rubricCode\":\"LESSON_TRANSLATION\",\"referenceTranslations\":[]}";

        var valid = GrammarExerciseRules.Validate(ExerciseType.Translation, validContent, validGrading, 10m, GradingMode.AiRubric);
        Assert.DoesNotContain(valid, i => i.Severity == "Error");

        var invalid = GrammarExerciseRules.Validate(ExerciseType.Translation, validContent, missingGrading, 10m, GradingMode.AiRubric);
        Assert.Contains(invalid, i => i.Severity == "Error");
    }

    [Fact]
    public void WritingPractice_ValidAndMissingAnswer()
    {
        const string validContent = "{\"prompt\":\"Write 3 sentences about your family.\"}";
        const string validGrading = "{\"rubricCode\":\"LESSON_WRITING\"}";
        const string missingContent = "{\"prompt\":\"\"}";

        var valid = GrammarExerciseRules.Validate(ExerciseType.WritingPractice, validContent, validGrading, 10m, GradingMode.AiRubric);
        Assert.DoesNotContain(valid, i => i.Severity == "Error");

        var invalid = GrammarExerciseRules.Validate(ExerciseType.WritingPractice, missingContent, validGrading, 10m, GradingMode.AiRubric);
        Assert.Contains(invalid, i => i.Severity == "Error");
    }

    [Fact]
    public void ReadingComprehension_ValidAndMissingAnswer()
    {
        const string validContent = """
        {
          "passage": "Passage text...",
          "subQuestions": [
            {
              "id": "q1",
              "questionKind": "SingleChoice",
              "maxPoints": 5,
              "options": [{"id":"o1","text":"Opt1"},{"id":"o2","text":"Opt2"}],
              "grading": {"correctOptionIds":["o1"]}
            },
            {
              "id": "q2",
              "questionKind": "TrueFalse",
              "maxPoints": 5,
              "grading": {"statementAnswer": true}
            }
          ]
        }
        """;

        var valid = GrammarExerciseRules.Validate(ExerciseType.ReadingComprehension, validContent, "{}", 10m, GradingMode.Composite);
        Assert.DoesNotContain(valid, i => i.Severity == "Error");

        // Missing passage
        const string missingPassage = """
        {
          "passage": "",
          "subQuestions": [
            {
              "id": "q1",
              "questionKind": "SingleChoice",
              "maxPoints": 5,
              "options": [{"id":"o1","text":"Opt1"},{"id":"o2","text":"Opt2"}],
              "grading": {"correctOptionIds":["o1"]}
            }
          ]
        }
        """;
        var invalidPassage = GrammarExerciseRules.Validate(ExerciseType.ReadingComprehension, missingPassage, "{}", 10m, GradingMode.Composite);
        Assert.Contains(invalidPassage, i => i.Severity == "Error" && i.Path.Contains("passage"));

        // ShortAnswer rejected
        const string shortAnswerContent = """
        {
          "passage": "Passage text...",
          "subQuestions": [
            {
              "id": "q1",
              "questionKind": "ShortAnswer",
              "maxPoints": 5,
              "grading": {}
            }
          ]
        }
        """;
        var invalidShortAnswer = GrammarExerciseRules.Validate(ExerciseType.ReadingComprehension, shortAnswerContent, "{}", 10m, GradingMode.Composite);
        Assert.Contains(invalidShortAnswer, i => i.Severity == "Error" && i.Message.Contains("ShortAnswer"));

        // Duplicate subQuestion ID
        const string duplicateQIdContent = """
        {
          "passage": "Passage text...",
          "subQuestions": [
            {
              "id": "q1",
              "questionKind": "TrueFalse",
              "maxPoints": 5,
              "grading": {"statementAnswer": true}
            },
            {
              "id": "q1",
              "questionKind": "TrueFalse",
              "maxPoints": 5,
              "grading": {"statementAnswer": false}
            }
          ]
        }
        """;
        var invalidDupId = GrammarExerciseRules.Validate(ExerciseType.ReadingComprehension, duplicateQIdContent, "{}", 10m, GradingMode.Composite);
        Assert.Contains(invalidDupId, i => i.Severity == "Error" && i.Message.Contains("Duplicate subQuestion id"));

        // SingleChoice with 2 correct option ids
        const string singleChoiceTwoAnswers = """
        {
          "passage": "Passage text...",
          "subQuestions": [
            {
              "id": "q1",
              "questionKind": "SingleChoice",
              "maxPoints": 10,
              "options": [{"id":"o1","text":"Opt1"},{"id":"o2","text":"Opt2"}],
              "grading": {"correctOptionIds":["o1","o2"]}
            }
          ]
        }
        """;
        var invalidSingleTwoAns = GrammarExerciseRules.Validate(ExerciseType.ReadingComprehension, singleChoiceTwoAnswers, "{}", 10m, GradingMode.Composite);
        Assert.Contains(invalidSingleTwoAns, i => i.Severity == "Error" && i.Message.Contains("exactly 1 correctOptionId"));

        // TrueFalse without boolean answer
        const string tfNoBool = """
        {
          "passage": "Passage text...",
          "subQuestions": [
            {
              "id": "q1",
              "questionKind": "TrueFalse",
              "maxPoints": 10,
              "grading": {"statementAnswer": "invalid"}
            }
          ]
        }
        """;
        var invalidTf = GrammarExerciseRules.Validate(ExerciseType.ReadingComprehension, tfNoBool, "{}", 10m, GradingMode.Composite);
        Assert.Contains(invalidTf, i => i.Severity == "Error" && i.Message.Contains("boolean"));

        // FillBlank without accepted answers
        const string fbNoAnswers = """
        {
          "passage": "Passage text...",
          "subQuestions": [
            {
              "id": "q1",
              "questionKind": "FillBlank",
              "maxPoints": 10,
              "grading": {"acceptedAnswers": []}
            }
          ]
        }
        """;
        var invalidFb = GrammarExerciseRules.Validate(ExerciseType.ReadingComprehension, fbNoAnswers, "{}", 10m, GradingMode.Composite);
        Assert.Contains(invalidFb, i => i.Severity == "Error" && i.Message.Contains("acceptedAnswers"));
    }

    [Fact]
    public void Validate_InvalidGradingMode_ReturnsError()
    {
        const string content = "{\"sourceText\":\"Test\"}";
        const string grading = "{\"rubricCode\":\"LESSON_TRANSLATION\",\"referenceTranslations\":[\"Test\"]}";

        // Translation with Deterministic instead of AiRubric
        var issues = GrammarExerciseRules.Validate(ExerciseType.Translation, content, grading, 10m, GradingMode.Deterministic);
        Assert.Contains(issues, i => i.Severity == "Error" && i.Path == "gradingMode");
    }

    public static IEnumerable<object[]> ValidExercisesForEndToEnd()
    {
        // 1. MultipleChoice
        yield return new object[]
        {
            ExerciseType.MultipleChoice,
            "{\"options\":[{\"id\":\"o1\",\"text\":\"A\"},{\"id\":\"o2\",\"text\":\"B\"}],\"selectionMode\":\"Single\"}",
            "{\"correctOptionIds\":[\"o1\"],\"explanation\":\"A is correct.\"}",
            "{\"selectedOptionIds\":[\"o1\"]}",
            GradingMode.Deterministic
        };

        // 2. TrueFalse
        yield return new object[]
        {
            ExerciseType.TrueFalse,
            "{\"statements\":[{\"id\":\"s1\",\"text\":\"Statement 1\"},{\"id\":\"s2\",\"text\":\"Statement 2\"}]}",
            "{\"statementAnswers\":{\"s1\":true,\"s2\":false}}",
            "{\"answers\":{\"s1\":true,\"s2\":false}}",
            GradingMode.Deterministic
        };

        // 3. Matching
        yield return new object[]
        {
            ExerciseType.Matching,
            "{\"leftItems\":[{\"id\":\"l1\",\"text\":\"Left 1\"},{\"id\":\"l2\",\"text\":\"Left 2\"}],\"rightItems\":[{\"id\":\"r1\",\"text\":\"Right 1\"},{\"id\":\"r2\",\"text\":\"Right 2\"}]}",
            "{\"correctPairs\":[{\"leftId\":\"l1\",\"rightId\":\"r1\"},{\"leftId\":\"l2\",\"rightId\":\"r2\"}]}",
            "{\"pairs\":[{\"leftId\":\"l1\",\"rightId\":\"r1\"},{\"leftId\":\"l2\",\"rightId\":\"r2\"}]}",
            GradingMode.Deterministic
        };

        // 4. FillInTheBlanks
        yield return new object[]
        {
            ExerciseType.FillInTheBlanks,
            "{\"textWithBlanks\":\"I [blank-1] a student and they [blank-2] teachers.\",\"blanks\":[{\"id\":\"blank-1\"},{\"id\":\"blank-2\"}]}",
            "{\"blankAnswers\":{\"blank-1\":{\"acceptedAnswers\":[\"am\"]},\"blank-2\":{\"acceptedAnswers\":[\"are\"]}}}",
            "{\"answers\":{\"blank-1\":\"am\",\"blank-2\":\"are\"}}",
            GradingMode.Deterministic
        };

        // 5. SentenceUnscramble
        yield return new object[]
        {
            ExerciseType.SentenceUnscramble,
            "{\"tokens\":[{\"id\":\"t1\",\"text\":\"She\"},{\"id\":\"t2\",\"text\":\"reads\"},{\"id\":\"t3\",\"text\":\"books\"}]}",
            "{\"validSequences\":[[\"t1\",\"t2\",\"t3\"]]}",
            "{\"orderedTokenIds\":[\"t1\",\"t2\",\"t3\"]}",
            GradingMode.Deterministic
        };

        // 6. ErrorCorrection
        yield return new object[]
        {
            ExerciseType.ErrorCorrection,
            "{\"incorrectSentence\":\"She have a cat.\"}",
            "{\"referenceCorrections\":[\"She has a cat.\"]}",
            "{\"correctedSentence\":\"She has a cat.\"}",
            GradingMode.Deterministic
        };

        // 7. ReadingComprehension (Composite)
        yield return new object[]
        {
            ExerciseType.ReadingComprehension,
            """
            {
              "passage": "Passage text about a student...",
              "subQuestions": [
                {
                  "id": "q1",
                  "questionKind": "SingleChoice",
                  "maxPoints": 5,
                  "options": [{"id":"o1","text":"Correct"},{"id":"o2","text":"Wrong"}],
                  "grading": {"correctOptionIds":["o1"]}
                },
                {
                  "id": "q2",
                  "questionKind": "TrueFalse",
                  "maxPoints": 5,
                  "grading": {"statementAnswer": true}
                }
              ]
            }
            """,
            "{}",
            """
            {
              "subQuestionAnswers": [
                {"questionId":"q1","selectedOptionIds":["o1"]},
                {"questionId":"q2","answer":true}
              ]
            }
            """,
            GradingMode.Composite
        };
    }

    [Theory]
    [MemberData(nameof(ValidExercisesForEndToEnd))]
    public void EndToEnd_ValidExercises_PassRulesValidation_AndGradeCorrectly(
        ExerciseType type,
        string contentJson,
        string gradingJson,
        string answerJson,
        GradingMode gradingMode)
    {
        // 1. Must pass rules validation with zero errors
        var issues = GrammarExerciseRules.Validate(type, contentJson, gradingJson, 10m, gradingMode);
        Assert.DoesNotContain(issues, i => i.Severity == "Error");

        // 2. Must grade correctly when evaluated by DeterministicExerciseGrader with correct answers
        var result = _grader.Grade(type, contentJson, gradingJson, answerJson, 10m);
        Assert.True(result.IsCorrect);
        Assert.Equal(100m, result.Score100);
        Assert.Equal(10m, result.PointsEarned);
    }
}
