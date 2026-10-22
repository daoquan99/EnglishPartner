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

            // Preserve Shadowing reference publicly
            if (type == ExerciseType.Shadowing)
            {
                if (obj["referenceText"] is null && obj["transcript"] is not null)
                {
                    obj["referenceText"] = obj["transcript"]!.DeepClone();
                }
            }

            // Remove answer keys and hidden teacher references that should never leak
            obj.Remove("correctOptionIds");
            obj.Remove("correctAnswers");
            obj.Remove("blankAnswers");
            obj.Remove("correctPairs");
            obj.Remove("statementAnswers");
            obj.Remove("referenceCorrections");
            obj.Remove("acceptedCorrections");
            obj.Remove("validSequences");
            obj.Remove("referenceTranslations");
            obj.Remove("acceptedSpellings");
            obj.Remove("acceptedVariants");
            obj.Remove("markingGuidance");

            // For spelling, hide target words
            if (type == ExerciseType.SpellingPractice)
            {
                obj.Remove("word");
                obj.Remove("targetWord");
                obj.Remove("correctAnswer");
                obj.Remove("audioText");
                obj.Remove("scrambledLetters");
            }

            // For dictation, listening comprehension, shadowing: hide hidden transcript
            if (type is ExerciseType.Dictation or ExerciseType.ListeningComprehension or ExerciseType.Shadowing)
            {
                obj.Remove("transcript");
                obj.Remove("referenceTranscript");
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
                            sqObj.Remove("statementAnswer");
                            sqObj.Remove("statementAnswers");
                            sqObj.Remove("acceptedAnswers");
                            sqObj.Remove("referenceAnswers");
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
