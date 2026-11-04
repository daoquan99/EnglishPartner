using System.Text.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Domain.Common.Exercises;

namespace EnglishLiveTutor.Application.Features.Grammar.Services;

public interface IGrammarLearnerContentSanitizer
{
    string Sanitize(ExerciseType type, string contentJson);
}

public sealed class GrammarLearnerContentSanitizer : IGrammarLearnerContentSanitizer
{
    public string Sanitize(ExerciseType type, string contentJson)
    {
        if (string.IsNullOrWhiteSpace(contentJson))
        {
            return "{}";
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(contentJson);
        }
        catch
        {
            return "{}";
        }

        if (node is not JsonObject sourceObj)
        {
            return "{}";
        }

        var result = new JsonObject();

        switch (type)
        {
            case ExerciseType.MultipleChoice:
                if (sourceObj["question"] != null) result["question"] = sourceObj["question"]!.DeepClone();
                if (sourceObj["selectionMode"] != null) result["selectionMode"] = sourceObj["selectionMode"]!.DeepClone();
                if (sourceObj["options"] is JsonArray mcOpts)
                {
                    var cleanOpts = new JsonArray();
                    foreach (var item in mcOpts)
                    {
                        if (item is JsonObject optObj)
                        {
                            var cleanOpt = new JsonObject();
                            if (optObj["id"] != null) cleanOpt["id"] = optObj["id"]!.DeepClone();
                            if (optObj["text"] != null) cleanOpt["text"] = optObj["text"]!.DeepClone();
                            cleanOpts.Add(cleanOpt);
                        }
                    }
                    result["options"] = cleanOpts;
                }
                break;

            case ExerciseType.TrueFalse:
                if (sourceObj["context"] != null) result["context"] = sourceObj["context"]!.DeepClone();
                if (sourceObj["statements"] is JsonArray tfStmts)
                {
                    var cleanStmts = new JsonArray();
                    foreach (var item in tfStmts)
                    {
                        if (item is JsonObject stmtObj)
                        {
                            var cleanStmt = new JsonObject();
                            if (stmtObj["id"] != null) cleanStmt["id"] = stmtObj["id"]!.DeepClone();
                            if (stmtObj["text"] != null) cleanStmt["text"] = stmtObj["text"]!.DeepClone();
                            cleanStmts.Add(cleanStmt);
                        }
                    }
                    result["statements"] = cleanStmts;
                }
                break;

            case ExerciseType.Matching:
                if (sourceObj["leftItems"] is JsonArray leftItems)
                {
                    var cleanLeft = new JsonArray();
                    foreach (var item in leftItems)
                    {
                        if (item is JsonObject leftObj)
                        {
                            var cleanItem = new JsonObject();
                            if (leftObj["id"] != null) cleanItem["id"] = leftObj["id"]!.DeepClone();
                            if (leftObj["text"] != null) cleanItem["text"] = leftObj["text"]!.DeepClone();
                            cleanLeft.Add(cleanItem);
                        }
                    }
                    result["leftItems"] = cleanLeft;
                }
                if (sourceObj["rightItems"] is JsonArray rightItems)
                {
                    var cleanRight = new JsonArray();
                    foreach (var item in rightItems)
                    {
                        if (item is JsonObject rightObj)
                        {
                            var cleanItem = new JsonObject();
                            if (rightObj["id"] != null) cleanItem["id"] = rightObj["id"]!.DeepClone();
                            if (rightObj["text"] != null) cleanItem["text"] = rightObj["text"]!.DeepClone();
                            cleanRight.Add(cleanItem);
                        }
                    }
                    result["rightItems"] = cleanRight;
                }
                break;

            case ExerciseType.FillInTheBlanks:
                if (sourceObj["textWithBlanks"] != null) result["textWithBlanks"] = sourceObj["textWithBlanks"]!.DeepClone();
                if (sourceObj["blanks"] is JsonArray blanks)
                {
                    var cleanBlanks = new JsonArray();
                    foreach (var item in blanks)
                    {
                        if (item is JsonObject blankObj)
                        {
                            var cleanBlank = new JsonObject();
                            if (blankObj["id"] != null) cleanBlank["id"] = blankObj["id"]!.DeepClone();
                            if (blankObj["hint"] != null) cleanBlank["hint"] = blankObj["hint"]!.DeepClone();
                            if (blankObj["maxLength"] != null) cleanBlank["maxLength"] = blankObj["maxLength"]!.DeepClone();
                            cleanBlanks.Add(cleanBlank);
                        }
                    }
                    result["blanks"] = cleanBlanks;
                }
                break;

            case ExerciseType.SentenceUnscramble:
                if (sourceObj["prompt"] != null) result["prompt"] = sourceObj["prompt"]!.DeepClone();
                if (sourceObj["tokens"] is JsonArray tokens)
                {
                    var cleanTokens = new JsonArray();
                    foreach (var item in tokens)
                    {
                        if (item is JsonObject tokenObj)
                        {
                            var cleanToken = new JsonObject();
                            if (tokenObj["id"] != null) cleanToken["id"] = tokenObj["id"]!.DeepClone();
                            if (tokenObj["text"] != null) cleanToken["text"] = tokenObj["text"]!.DeepClone();
                            cleanTokens.Add(cleanToken);
                        }
                    }
                    result["tokens"] = cleanTokens;
                }
                break;

            case ExerciseType.ErrorCorrection:
                if (sourceObj["incorrectSentence"] != null) result["incorrectSentence"] = sourceObj["incorrectSentence"]!.DeepClone();
                if (sourceObj["instructions"] != null) result["instructions"] = sourceObj["instructions"]!.DeepClone();
                break;

            case ExerciseType.Translation:
                if (sourceObj["direction"] != null) result["direction"] = sourceObj["direction"]!.DeepClone();
                if (sourceObj["sourceText"] != null) result["sourceText"] = sourceObj["sourceText"]!.DeepClone();
                if (sourceObj["context"] != null) result["context"] = sourceObj["context"]!.DeepClone();
                break;

            case ExerciseType.WritingPractice:
                if (sourceObj["prompt"] != null) result["prompt"] = sourceObj["prompt"]!.DeepClone();
                if (sourceObj["minWords"] != null) result["minWords"] = sourceObj["minWords"]!.DeepClone();
                if (sourceObj["maxWords"] != null) result["maxWords"] = sourceObj["maxWords"]!.DeepClone();
                break;

            case ExerciseType.ReadingComprehension:
                if (sourceObj["passage"] != null) result["passage"] = sourceObj["passage"]!.DeepClone();
                if (sourceObj["subQuestions"] is JsonArray subQuestions)
                {
                    var cleanSubQuestions = new JsonArray();
                    foreach (var item in subQuestions)
                    {
                        if (item is JsonObject subQObj)
                        {
                            var cleanSubQ = new JsonObject();
                            if (subQObj["id"] != null) cleanSubQ["id"] = subQObj["id"]!.DeepClone();
                            if (subQObj["questionKind"] != null) cleanSubQ["questionKind"] = subQObj["questionKind"]!.DeepClone();
                            if (subQObj["order"] != null) cleanSubQ["order"] = subQObj["order"]!.DeepClone();
                            if (subQObj["maxPoints"] != null) cleanSubQ["maxPoints"] = subQObj["maxPoints"]!.DeepClone();
                            if (subQObj["questionText"] != null) cleanSubQ["questionText"] = subQObj["questionText"]!.DeepClone();
                            if (subQObj["options"] is JsonArray subOptions)
                            {
                                var cleanOpts = new JsonArray();
                                foreach (var opt in subOptions)
                                {
                                    if (opt is JsonObject optObj)
                                    {
                                        var cleanOpt = new JsonObject();
                                        if (optObj["id"] != null) cleanOpt["id"] = optObj["id"]!.DeepClone();
                                        if (optObj["text"] != null) cleanOpt["text"] = optObj["text"]!.DeepClone();
                                        cleanOpts.Add(cleanOpt);
                                    }
                                }
                                cleanSubQ["options"] = cleanOpts;
                            }
                            cleanSubQuestions.Add(cleanSubQ);
                        }
                    }
                    result["subQuestions"] = cleanSubQuestions;
                }
                break;

            default:
                break;
        }

        return result.ToJsonString();
    }
}
