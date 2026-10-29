using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Rules;
using EnglishLiveTutor.Application.Features.Grammar.Services;
using EnglishLiveTutor.Domain.Lessons.Enums;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class GrammarExerciseOutputProcessorTests
{
    private readonly GrammarExerciseOutputProcessor _processor = new();

    [Fact]
    public void Process_WithCodeFence_ShouldStripCodeFenceAndParseSuccessfully()
    {
        string raw = @"```json
{
  ""exercises"": [
    {
      ""type"": ""MultipleChoice"",
      ""difficulty"": 1,
      ""title"": ""To be Affirmative"",
      ""instructions"": ""Chọn đáp án đúng."",
      ""content"": {
        ""question"": ""She ____ a teacher."",
        ""selectionMode"": ""Single"",
        ""options"": [
          { ""id"": ""opt-a"", ""text"": ""am"" },
          { ""id"": ""opt-b"", ""text"": ""is"" },
          { ""id"": ""opt-c"", ""text"": ""are"" }
        ]
      },
      ""grading"": {
        ""correctOptionIds"": [""opt-b""],
        ""explanation"": ""Chủ ngữ 'she' đi với 'is'.""
      }
    }
  ]
}
```";

        var requested = new List<GrammarExercisePromptRequestedItem>
        {
            new(ExerciseType.MultipleChoice, 1, 1)
        };
        var existingHashes = new HashSet<string>();

        var (valid, rejected) = _processor.Process(raw, requested, existingHashes);

        Assert.Empty(rejected);
        Assert.Single(valid);
        Assert.Equal(ExerciseType.MultipleChoice, valid[0].Type);
        Assert.Equal(1, valid[0].Difficulty);
    }

    [Fact]
    public void Process_UnrequestedType_ShouldBeRejected()
    {
        string raw = @"{
  ""exercises"": [
    {
      ""type"": ""TrueFalse"",
      ""difficulty"": 1,
      ""title"": ""True or False"",
      ""instructions"": ""Đúng hay sai?"",
      ""content"": {
        ""context"": ""Kiểm tra ngữ pháp"",
        ""statements"": [
          { ""id"": ""s-1"", ""text"": ""She are happy."" }
        ]
      },
      ""grading"": {
        ""statementAnswers"": { ""s-1"": false },
        ""explanation"": ""She đi với is.""
      }
    }
  ]
}";

        var requested = new List<GrammarExercisePromptRequestedItem>
        {
            new(ExerciseType.MultipleChoice, 2, 1)
        };
        var existingHashes = new HashSet<string>();

        var (valid, rejected) = _processor.Process(raw, requested, existingHashes);

        Assert.Empty(valid);
        Assert.Single(rejected);
        Assert.Contains("not requested", rejected[0].Issues[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Process_SurplusItemsExceedingCount_ShouldBeCut()
    {
        string raw = @"{
  ""exercises"": [
    {
      ""type"": ""MultipleChoice"",
      ""difficulty"": 1,
      ""title"": ""MC 1"",
      ""instructions"": ""Chọn đáp án"",
      ""content"": {
        ""question"": ""I ____ a student."",
        ""selectionMode"": ""Single"",
        ""options"": [{ ""id"": ""opt-a"", ""text"": ""am"" }, { ""id"": ""opt-b"", ""text"": ""is"" }]
      },
      ""grading"": {
        ""correctOptionIds"": [""opt-a""],
        ""explanation"": ""I đi với am""
      }
    },
    {
      ""type"": ""MultipleChoice"",
      ""difficulty"": 1,
      ""title"": ""MC 2"",
      ""instructions"": ""Chọn đáp án"",
      ""content"": {
        ""question"": ""You ____ a student."",
        ""selectionMode"": ""Single"",
        ""options"": [{ ""id"": ""opt-a"", ""text"": ""are"" }, { ""id"": ""opt-b"", ""text"": ""is"" }]
      },
      ""grading"": {
        ""correctOptionIds"": [""opt-a""],
        ""explanation"": ""You đi với are""
      }
    }
  ]
}";

        // Only requested count 1
        var requested = new List<GrammarExercisePromptRequestedItem>
        {
            new(ExerciseType.MultipleChoice, 1, 1)
        };
        var existingHashes = new HashSet<string>();

        var (valid, rejected) = _processor.Process(raw, requested, existingHashes);

        // First item is taken, second item is cut (not stored)
        Assert.Single(valid);
        Assert.Equal("MC 1", valid[0].Title);
    }

    [Fact]
    public void Process_InvalidSchema_ShouldBeRejectedWithIssues()
    {
        // Content contains answer keys forbidden in content
        string raw = @"{
  ""exercises"": [
    {
      ""type"": ""MultipleChoice"",
      ""difficulty"": 1,
      ""title"": ""Bad MC"",
      ""instructions"": ""Chọn đáp án"",
      ""content"": {
        ""question"": ""She is happy."",
        ""correctOptionIds"": [""opt-a""], // Forbidden in content
        ""options"": [{ ""id"": ""opt-a"", ""text"": ""yes"" }]
      },
      ""grading"": {
        ""correctOptionIds"": [""opt-a""]
      }
    }
  ]
}";

        var requested = new List<GrammarExercisePromptRequestedItem>
        {
            new(ExerciseType.MultipleChoice, 1, 1)
        };
        var existingHashes = new HashSet<string>();

        var (valid, rejected) = _processor.Process(raw, requested, existingHashes);

        Assert.Empty(valid);
        Assert.Single(rejected);
        Assert.Contains(rejected[0].Issues, i => i.Contains("correctOptionIds"));
    }

    [Fact]
    public void Process_DuplicateHashWithOfficialOrApproved_ShouldBeRejected()
    {
        string contentJson = @"{""question"":""She ____ a doctor."",""selectionMode"":""Single"",""options"":[{""id"":""opt-a"",""text"":""is""},{""id"":""opt-b"",""text"":""are""}]}";
        string gradingJson = @"{""correctOptionIds"":[""opt-a""],""explanation"":""She is""}";

        string knownHash = GrammarContentHasher.ComputeHash(ExerciseType.MultipleChoice, contentJson, gradingJson);

        string raw = $@"{{
  ""exercises"": [
    {{
      ""type"": ""MultipleChoice"",
      ""difficulty"": 1,
      ""title"": ""Duplicate MC"",
      ""instructions"": ""Chọn đáp án"",
      ""content"": {contentJson},
      ""grading"": {gradingJson}
    }}
  ]
}}";

        var requested = new List<GrammarExercisePromptRequestedItem>
        {
            new(ExerciseType.MultipleChoice, 1, 1)
        };

        // Existing hashes contains the hash of an official/approved exercise
        var existingHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { knownHash };

        var (valid, rejected) = _processor.Process(raw, requested, existingHashes);

        Assert.Empty(valid);
        Assert.Single(rejected);
        Assert.Contains("Duplicate", rejected[0].Issues[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Process_DifficultyOutside1To3_ShouldDefaultTo2_AndFocusTruncatedTo200()
    {
        string raw = @"{
  ""exercises"": [
    {
      ""type"": ""MultipleChoice"",
      ""difficulty"": 99,
      ""focus"": """ + new string('X', 250) + @""",
      ""title"": ""Difficulty test"",
      ""instructions"": ""Test"",
      ""content"": {
        ""question"": ""They ____ here."",
        ""selectionMode"": ""Single"",
        ""options"": [{ ""id"": ""opt-a"", ""text"": ""are"" }, { ""id"": ""opt-b"", ""text"": ""is"" }]
      },
      ""grading"": {
        ""correctOptionIds"": [""opt-a""],
        ""explanation"": ""They are""
      }
    }
  ]
}";

        var requested = new List<GrammarExercisePromptRequestedItem>
        {
            new(ExerciseType.MultipleChoice, 1, 2)
        };
        var existingHashes = new HashSet<string>();

        var (valid, rejected) = _processor.Process(raw, requested, existingHashes);

        Assert.Empty(rejected);
        Assert.Single(valid);
        Assert.Equal(2, valid[0].Difficulty);
        Assert.NotNull(valid[0].Focus);
        Assert.Equal(200, valid[0].Focus!.Length);
    }
}
