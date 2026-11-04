using EnglishLiveTutor.Application.Features.Grammar.Rules;
using EnglishLiveTutor.Domain.Common.Exercises;

namespace EnglishLiveTutor.Domain.Tests;

public class GrammarContentHasherTests
{
    [Fact]
    public void ComputeHash_DifferentKeyOrder_ProducesSameHash()
    {
        const string content1 = "{\"options\":[{\"id\":\"o1\",\"text\":\"A\"}],\"title\":\"Hello\",\"level\":1}";
        const string content2 = "{\"title\":\"Hello\",\"level\":1,\"options\":[{\"id\":\"o1\",\"text\":\"A\"}]}";
        const string grading = "{\"correctOptionIds\":[\"o1\"]}";

        string hash1 = GrammarContentHasher.ComputeHash(ExerciseType.MultipleChoice, content1, grading);
        string hash2 = GrammarContentHasher.ComputeHash(ExerciseType.MultipleChoice, content2, grading);

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length);
    }

    [Fact]
    public void ComputeHash_WhitespaceAndCasing_ProducesSameHash()
    {
        const string content1 = "{\"sentence\":\"  She   is a TEACHER.  \"}";
        const string content2 = "{\"sentence\":\"she is a teacher.\"}";
        const string grading = "{\"answers\":[\"teacher\"]}";

        string hash1 = GrammarContentHasher.ComputeHash(ExerciseType.FillInTheBlanks, content1, grading);
        string hash2 = GrammarContentHasher.ComputeHash(ExerciseType.FillInTheBlanks, content2, grading);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DifferentAnswers_ProducesDifferentHash()
    {
        const string content = "{\"sentence\":\"She is a teacher.\"}";
        const string grading1 = "{\"correctOptionIds\":[\"o1\"]}";
        const string grading2 = "{\"correctOptionIds\":[\"o2\"]}";

        string hash1 = GrammarContentHasher.ComputeHash(ExerciseType.MultipleChoice, content, grading1);
        string hash2 = GrammarContentHasher.ComputeHash(ExerciseType.MultipleChoice, content, grading2);

        Assert.NotEqual(hash1, hash2);
    }
}
