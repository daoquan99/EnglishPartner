using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Lessons.Enums;

namespace EnglishLiveTutor.Infrastructure.Lessons;

public sealed class LessonSanitizer : ILessonSanitizer
{
    public string SanitizeContentForLearner(ExerciseType type, string contentJson)
    {
        if (string.IsNullOrWhiteSpace(contentJson))
            return "{}";

        try
        {
            var node = JsonNode.Parse(contentJson);
            if (node is not JsonObject obj)
                return contentJson;

            if (type == ExerciseType.Shadowing && obj["referenceText"] is null && obj["transcript"] is not null)
                obj["referenceText"] = obj["transcript"]!.DeepClone();

            // Remove any leaked grading properties from content if mistakenly placed there
            obj.Remove("correctOptionIds");
            obj.Remove("explanation");
            obj.Remove("correctAnswers");
            obj.Remove("acceptedCorrections");
            obj.Remove("validSequences");
            obj.Remove("transcript");
            obj.Remove("rubricCode");
            obj.Remove("rubricVersion");
            obj.Remove("taskInstructions");
            obj.Remove("taskSpecificGuidance");
            obj.Remove("markingGuidance");
            if (type == ExerciseType.SpellingPractice)
            {
                obj.Remove("word");
                obj.Remove("targetWord");
                obj.Remove("audioText");
                obj.Remove("scrambledLetters");
            }

            // For composite exercises (Reading/Listening Comprehension), sanitize subQuestions
            if (type is ExerciseType.ReadingComprehension or ExerciseType.ListeningComprehension)
            {
                if (obj.TryGetPropertyValue("subQuestions", out var subQNode) && subQNode is JsonArray subQArray)
                {
                    foreach (var sq in subQArray)
                    {
                        if (sq is JsonObject sqObj)
                        {
                            sqObj.Remove("grading");
                            sqObj.Remove("correctOptionIds");
                            sqObj.Remove("correctAnswers");
                            sqObj.Remove("answers");
                            sqObj.Remove("explanation");
                            sqObj.Remove("rubricCode");
                            sqObj.Remove("rubricVersion");
                        }
                    }
                }
            }

            return obj.ToJsonString();
        }
        catch
        {
            return contentJson;
        }
    }
}
