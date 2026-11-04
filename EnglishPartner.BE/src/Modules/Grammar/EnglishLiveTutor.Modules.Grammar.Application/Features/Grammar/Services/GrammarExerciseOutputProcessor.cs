using System.Text.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Rules;
using EnglishLiveTutor.Domain.Common.Exercises;

namespace EnglishLiveTutor.Application.Features.Grammar.Services;

public sealed record ProcessedExerciseDto(
    ExerciseType Type,
    int Difficulty,
    string Title,
    string Instructions,
    string? Focus,
    string ContentJson,
    string? GradingDefinitionJson,
    decimal MaxPoints,
    GradingMode GradingMode,
    string ContentHash
);

public interface IGrammarExerciseOutputProcessor
{
    (IReadOnlyList<ProcessedExerciseDto> Valid, IReadOnlyList<GrammarExerciseRejectionDto> Rejected) Process(
        string rawOutput,
        IReadOnlyList<GrammarExercisePromptRequestedItem>? expectedItems,
        ISet<string> existingHashes);
}

public sealed class GrammarExerciseOutputProcessor : IGrammarExerciseOutputProcessor
{
    public (IReadOnlyList<ProcessedExerciseDto> Valid, IReadOnlyList<GrammarExerciseRejectionDto> Rejected) Process(
        string rawOutput,
        IReadOnlyList<GrammarExercisePromptRequestedItem>? expectedItems,
        ISet<string> existingHashes)
    {
        var valid = new List<ProcessedExerciseDto>();
        var rejected = new List<GrammarExerciseRejectionDto>();

        string cleanJson = StripCodeFence(rawOutput);
        if (string.IsNullOrWhiteSpace(cleanJson))
        {
            rejected.Add(new GrammarExerciseRejectionDto(0, ["Empty AI output."]));
            return (valid, rejected);
        }

        JsonObject? rootObj;
        try
        {
            var node = JsonNode.Parse(cleanJson, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            rootObj = node as JsonObject;
            if (rootObj == null)
            {
                rejected.Add(new GrammarExerciseRejectionDto(0, ["Output root must be a JSON object containing 'exercises' array."]));
                return (valid, rejected);
            }
        }
        catch (JsonException ex)
        {
            rejected.Add(new GrammarExerciseRejectionDto(0, [$"Malformed JSON output: {ex.Message}"]));
            return (valid, rejected);
        }

        if (rootObj["exercises"] is not JsonArray exercisesArr || exercisesArr.Count == 0)
        {
            rejected.Add(new GrammarExerciseRejectionDto(0, ["Output must contain a non-empty 'exercises' array."]));
            return (valid, rejected);
        }

        // Determine expected items count and allowed types
        HashSet<ExerciseType>? allowedTypes = null;
        Dictionary<ExerciseType, int>? remainingCountPerType = null;

        if (expectedItems != null && expectedItems.Count > 0)
        {
            allowedTypes = expectedItems.Select(i => i.Type).ToHashSet();
            remainingCountPerType = expectedItems
                .GroupBy(i => i.Type)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));
        }

        for (int i = 0; i < exercisesArr.Count; i++)
        {
            var itemNode = exercisesArr[i];
            if (itemNode is not JsonObject itemObj)
            {
                rejected.Add(new GrammarExerciseRejectionDto(i, ["Exercise item must be a JSON object."]));
                continue;
            }

            // 1. Type
            string typeStr = itemObj["type"]?.ToString() ?? string.Empty;
            if (!Enum.TryParse<ExerciseType>(typeStr, ignoreCase: true, out var type) ||
                !GrammarExerciseRules.AllowedTypes.Contains(type))
            {
                rejected.Add(new GrammarExerciseRejectionDto(i, [$"Invalid or unsupported exercise type: '{typeStr}'."]));
                continue;
            }

            if (allowedTypes != null && !allowedTypes.Contains(type))
            {
                rejected.Add(new GrammarExerciseRejectionDto(i, [$"Exercise type '{type}' was not requested."]));
                continue;
            }

            // Cut off extra items exceeding requested count per type
            if (remainingCountPerType != null &&
                remainingCountPerType.TryGetValue(type, out int remaining) &&
                remaining <= 0)
            {
                // Surplus item for this type -> skip/cut
                continue;
            }

            // 2. Difficulty (outside 1..3 -> 2)
            int difficulty = 2;
            if (itemObj.TryGetPropertyValue("difficulty", out var diffNode) &&
                diffNode is JsonValue diffVal &&
                diffVal.TryGetValue<int>(out var parsedDiff))
            {
                difficulty = (parsedDiff >= 1 && parsedDiff <= 3) ? parsedDiff : 2;
            }

            // 3. Focus (<= 200 chars)
            string? focus = itemObj["focus"]?.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(focus) && focus.Length > 200)
            {
                focus = focus[..200];
            }
            if (string.IsNullOrWhiteSpace(focus))
            {
                focus = null;
            }

            // 4. Title & Instructions
            string title = itemObj["title"]?.ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                title = $"{type} Exercise";
            }
            else if (title.Length > 200)
            {
                title = title[..200];
            }

            string instructions = itemObj["instructions"]?.ToString()?.Trim() ?? string.Empty;
            if (instructions.Length > 1000)
            {
                instructions = instructions[..1000];
            }

            // 5. GradingMode
            GradingMode gradingMode;
            if (GrammarExerciseRules.DeterministicTypes.Contains(type))
            {
                gradingMode = GradingMode.Deterministic;
            }
            else if (type == ExerciseType.Translation || type == ExerciseType.WritingPractice)
            {
                gradingMode = GradingMode.AiRubric;
            }
            else if (type == ExerciseType.ReadingComprehension)
            {
                gradingMode = GradingMode.Composite;
            }
            else
            {
                gradingMode = GradingMode.Deterministic;
            }

            // 6. Content & Grading JSON
            string contentJson = itemObj["content"]?.ToJsonString() ?? "{}";
            string? gradingJson = itemObj["grading"]?.ToJsonString();
            if (string.IsNullOrWhiteSpace(gradingJson) || gradingJson.Trim() == "null")
            {
                gradingJson = null;
            }

            // 7. Validate via GrammarExerciseRules
            decimal maxPoints = 10m;
            var issues = GrammarExerciseRules.Validate(type, contentJson, gradingJson, maxPoints, gradingMode);
            if (issues.Count > 0)
            {
                rejected.Add(new GrammarExerciseRejectionDto(i, issues.Select(x => x.Message).ToList()));
                continue;
            }

            // 8. Deduplicate hash against existing hashes (Official, Approved, owner's, and accepted in current batch)
            string hash = GrammarContentHasher.ComputeHash(type, contentJson, gradingJson);
            if (existingHashes.Contains(hash))
            {
                rejected.Add(new GrammarExerciseRejectionDto(i, ["Duplicate exercise content."]));
                continue;
            }

            // Valid exercise
            existingHashes.Add(hash);
            if (remainingCountPerType != null && remainingCountPerType.ContainsKey(type))
            {
                remainingCountPerType[type]--;
            }

            valid.Add(new ProcessedExerciseDto(
                Type: type,
                Difficulty: difficulty,
                Title: title,
                Instructions: instructions,
                Focus: focus,
                ContentJson: contentJson,
                GradingDefinitionJson: gradingJson,
                MaxPoints: maxPoints,
                GradingMode: gradingMode,
                ContentHash: hash
            ));
        }

        return (valid, rejected);
    }

    public static string StripCodeFence(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        string text = raw.Trim();

        if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            text = text[7..];
        }
        else if (text.StartsWith("```"))
        {
            text = text[3..];
        }

        if (text.EndsWith("```"))
        {
            text = text[..^3];
        }

        return text.Trim();
    }
}
