using System.Net.Http.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Pronunciation;

namespace EnglishLiveTutor.Infrastructure.Ai;

public sealed class GeminiPronunciationAiAssessor : IPronunciationAiAssessor
{
    private readonly HttpClient _http;

    public GeminiPronunciationAiAssessor(HttpClient http)
    {
        _http = http;
    }

    public async Task<PronunciationAiRawResult> AssessAudioAsync(
        string apiKey,
        string modelCode,
        string base64Audio,
        string contentType,
        string expectedText,
        string accent,
        ScoringRubricEntity rubric,
        CancellationToken ct = default)
    {
        var enabledCriteria = rubric.Criteria.Where(c => c.IsEnabled).OrderBy(c => c.DisplayOrder).ToList();
        string criteriaDescLines = string.Join("\n", enabledCriteria.Select(c =>
            $"- {c.Code} ({c.Name}, weight {c.Weight}%): {c.Description} (Scoring: {c.MinimumScore}-{c.MaximumScore})"));

        string systemPrompt = $@"You are an elite, native English phonetics and pronunciation expert tutor assessing a Vietnamese ESL learner's speech.
Task: Objectively evaluate the learner's pronunciation against the expected text in the target accent.

Expected Text: ""{expectedText}""
Target Accent: {accent}

Active Scoring Rubric ({rubric.Code} v{rubric.Version}):
{criteriaDescLines}

Rules:
1. Listen carefully to the user's audio. If the audio is completely silent, pure noise, or unintelligible, set ""isAssessable"": false and provide helpful Vietnamese guidance in ""improvements"".
2. Otherwise set ""isAssessable"": true, transcribe what the user actually said in ""recognizedText"", and score each criterion from 0 to 100.
3. Provide concise, actionable feedback in Vietnamese (`feedback`, `strengths`, `improvements`, `suggestedPractice`).
4. Return ONLY valid JSON matching this exact structure:
{{
  ""targetType"": ""Word"",
  ""expectedText"": ""{expectedText}"",
  ""recognizedText"": ""string"",
  ""accent"": ""{accent}"",
  ""isAssessable"": true,
  ""confidence"": 0.92,
  ""criteria"": [
    {{ ""code"": ""Accuracy"", ""score"": 85, ""feedback"": ""Nhận xét tiếng Việt..."" }},
    {{ ""code"": ""Clarity"", ""score"": 90, ""feedback"": ""Nhận xét tiếng Việt..."" }},
    {{ ""code"": ""Fluency"", ""score"": 82, ""feedback"": ""Nhận xét tiếng Việt..."" }}
  ],
  ""strengths"": [""Điểm phát âm tốt bằng tiếng Việt""],
  ""improvements"": [""Điểm cần cải thiện bằng tiếng Việt""],
  ""suggestedPractice"": [""Bài tập luyện phát âm cụ thể bằng tiếng Việt""]
}}";

        string cleanMime = (contentType ?? "audio/webm").Split(';')[0].Trim();
        if (string.IsNullOrWhiteSpace(cleanMime)) cleanMime = "audio/webm";

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelCode}:generateContent?key={Uri.EscapeDataString(apiKey.Trim())}";

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = systemPrompt },
                        new
                        {
                            inlineData = new
                            {
                                mimeType = cleanMime,
                                data = base64Audio
                            }
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                responseMimeType = "application/json"
            }
        };

        using var resp = await _http.PostAsJsonAsync(url, payload, ct);
        string rawBody = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Google AI Studio trả về lỗi {(int)resp.StatusCode} khi chấm phát âm: {rawBody}");
        }

        var root = JsonNode.Parse(rawBody);
        string? jsonText = root?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(jsonText))
        {
            throw new InvalidOperationException("AI không trả về kết quả chấm phát âm hợp lệ.");
        }

        jsonText = CleanJsonFence(jsonText);
        var parsed = JsonNode.Parse(jsonText);
        if (parsed is null)
        {
            throw new InvalidOperationException("Không thể phân tích JSON chấm điểm phát âm.");
        }

        bool isAssessable = parsed["isAssessable"]?.GetValue<bool>() ?? true;
        string? recognizedText = parsed["recognizedText"]?.GetValue<string>()?.Trim();
        decimal confidence = Math.Clamp(parsed["confidence"]?.GetValue<decimal>() ?? 0.85m, 0m, 1m);

        var strengths = ReadStringArray(parsed["strengths"]);
        var improvements = ReadStringArray(parsed["improvements"]);
        var suggestedPractice = ReadStringArray(parsed["suggestedPractice"]);

        var criterionDict = new Dictionary<string, (decimal RawScore, string Feedback)>(StringComparer.OrdinalIgnoreCase);
        var aiCriteriaArray = parsed["criteria"]?.AsArray();

        if (aiCriteriaArray is not null)
        {
            foreach (var item in aiCriteriaArray)
            {
                if (item is null) continue;
                string code = item["code"]?.GetValue<string>()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(code)) continue;

                decimal score = isAssessable ? (item["score"]?.GetValue<decimal>() ?? 75m) : 0m;
                string feedback = item["feedback"]?.GetValue<string>()?.Trim()
                    ?? (isAssessable ? "Phát âm khá rõ ràng." : "Không nhận diện được giọng nói rõ ràng.");

                criterionDict[code] = (score, feedback);
            }
        }

        return new PronunciationAiRawResult(
            RecognizedText: recognizedText,
            Confidence: confidence,
            IsAssessable: isAssessable,
            CriterionScores: criterionDict,
            Strengths: strengths,
            Improvements: improvements,
            SuggestedPractice: suggestedPractice
        );
    }

    private static List<string> ReadStringArray(JsonNode? node)
    {
        var list = new List<string>();
        if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                string? s = item?.GetValue<string>()?.Trim();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    list.Add(s);
                }
            }
        }
        return list;
    }

    private static string CleanJsonFence(string raw)
    {
        string t = raw.Trim();
        if (t.StartsWith("```"))
        {
            int firstNewline = t.IndexOf('\n');
            if (firstNewline >= 0) t = t[(firstNewline + 1)..];
            if (t.EndsWith("```")) t = t[..^3];
        }
        return t.Trim();
    }
}
