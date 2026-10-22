using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Lessons.Enums;

namespace EnglishLiveTutor.Infrastructure.Lessons.PromptBuilder;

public sealed class LessonPromptBuilder : ILessonPromptBuilder
{
    private static readonly ConcurrentDictionary<string, (LessonPromptResultDto Result, DateTimeOffset CachedAt)> PromptCache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    private readonly ILessonPromptTemplateProvider _templateProvider;

    public LessonPromptBuilder(ILessonPromptTemplateProvider templateProvider)
    {
        _templateProvider = templateProvider;
    }

    public Task<LessonPromptResultDto> BuildPromptAsync(LessonPromptInput input, CancellationToken ct = default)
    {
        string topic = string.IsNullOrWhiteSpace(input.Topic) ? "General English" : input.Topic.Trim();
        string cefr = NormalizeCefr(input.CefrLevel);
        string title = !string.IsNullOrWhiteSpace(input.LessonTitle) ? input.LessonTitle.Trim() : $"English Lesson: {topic}";
        int targetCount = Math.Clamp(input.TargetExerciseCount <= 0 ? 5 : input.TargetExerciseCount, 1, 30);
        string? customInstructions = string.IsNullOrWhiteSpace(input.CustomInstructions) ? null : input.CustomInstructions.Trim();

        var preferredTypes = NormalizeExerciseTypes(input.PreferredExerciseTypes);

        string cacheKey = ComputeCacheKey(topic, cefr, title, targetCount, preferredTypes, customInstructions);
        var now = DateTimeOffset.UtcNow;

        if (PromptCache.TryGetValue(cacheKey, out var entry) && entry.CachedAt.Add(CacheTtl) > now)
        {
            return Task.FromResult(entry.Result);
        }

        string exerciseRules = _templateProvider.BuildExerciseTypeRules(preferredTypes, targetCount);
        string exerciseSpecs = _templateProvider.BuildExerciseTypeSpecifications(preferredTypes);
        string exerciseExamples = _templateProvider.BuildConcreteExerciseExamples(preferredTypes, topic, cefr);

        string systemPrompt = @"You are an expert ESL Curriculum Designer. Your task is to generate complete, high-quality English lessons matching schema 'lesson-exercises.v1'.
Always return valid JSON adhering strictly to the Lesson Exercises JSON Schema with exact PascalCase ExerciseType names and compliant content/grading payloads.";

        string userPrompt = $@"Generate an English lesson with interactive exercises:
- Title: {title}
- Topic: {topic}
- CEFR Level: {cefr}
- Target Exercise Count: {targetCount}
- Preferred Exercise Types: {string.Join(", ", preferredTypes)}
{(customInstructions == null ? "" : $"- Teacher Custom Instructions: {customInstructions}")}

CRITICAL RULES FOR EXERCISES:
{exerciseRules}

DETAILED SPECIFICATIONS FOR REQUESTED TYPES:
{exerciseSpecs}

ENVELOPE JSON STRUCTURE & EXAMPLES:
{{
  ""$schema"": ""https://raw.githubusercontent.com/.../lesson-exercises-v1.schema.json"",
  ""schemaVersion"": ""lesson-exercises.v1"",
  ""lesson"": {{
    ""title"": ""{title}"",
    ""cefrLevel"": ""{cefr}"",
    ""topic"": ""{topic}"",
    ""learningObjectives"": [
      ""Understand core vocabulary and phrases for {topic}"",
      ""Construct natural English sentences appropriate for {cefr} level""
    ],
    ""targetVocabulary"": [""daily routine"", ""wake up"", ""commute"", ""work remotely"", ""attend meetings"", ""schedule"", ""work-life balance""],
    ""targetGrammar"": [""Present Simple"", ""Adverbs of Frequency"", ""Time Expressions""],
    ""content"": ""This lesson focuses on practical language for {topic} at CEFR {cefr} level."",
    ""estimatedMinutes"": 30
  }},
  ""exercises"": [
{exerciseExamples}
  ]
}}";

        string promptText = $@"{systemPrompt}

{userPrompt}";

        var result = new LessonPromptResultDto(
            SystemPrompt: systemPrompt,
            UserPrompt: userPrompt,
            PromptText: promptText,
            SchemaVersion: "lesson-exercises.v1");

        // Maintain bounded cache
        if (PromptCache.Count > 500)
        {
            PromptCache.Clear();
        }

        PromptCache[cacheKey] = (result, now);
        return Task.FromResult(result);
    }

    private static string NormalizeCefr(string? cefr)
    {
        if (string.IsNullOrWhiteSpace(cefr)) return "B1";
        string upper = cefr.Trim().ToUpperInvariant();
        return upper is "A1" or "A2" or "B1" or "B2" or "C1" or "C2" ? upper : "B1";
    }

    private static List<string> NormalizeExerciseTypes(IReadOnlyList<string>? types)
    {
        var validEnumNames = new HashSet<string>(Enum.GetNames<ExerciseType>(), StringComparer.OrdinalIgnoreCase);

        if (types == null || types.Count == 0)
        {
            return new List<string> { "Flashcards", "MultipleChoice", "FillInTheBlanks", "SentenceUnscramble", "Translation" };
        }

        var list = new List<string>();
        foreach (var t in types)
        {
            if (!string.IsNullOrWhiteSpace(t))
            {
                string trimmed = t.Trim();
                if (validEnumNames.Contains(trimmed) && !list.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                {
                    // Map to canonical PascalCase
                    var canonical = Enum.Parse<ExerciseType>(trimmed, ignoreCase: true).ToString();
                    list.Add(canonical);
                }
            }
        }

        return list.Count > 0
            ? list
            : new List<string> { "Flashcards", "MultipleChoice", "FillInTheBlanks", "SentenceUnscramble", "Translation" };
    }

    private string ComputeCacheKey(
        string topic,
        string cefr,
        string title,
        int count,
        List<string> types,
        string? customInstructions)
    {
        string raw = $"{_templateProvider.TemplateVersion}:{cefr}:{topic.ToLowerInvariant()}:{title}:{count}:{string.Join(",", types)}:{customInstructions ?? ""}";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(hash);
    }
}
