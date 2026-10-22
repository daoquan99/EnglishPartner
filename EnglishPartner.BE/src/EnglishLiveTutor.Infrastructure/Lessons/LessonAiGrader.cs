using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Lessons.Enums;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Entities;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;
using EnglishLiveTutor.Infrastructure.Ai;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Infrastructure.Lessons;

public sealed class LessonAiGrader : ILessonAiGrader
{
    private readonly HttpClient _http;
    private readonly IAudioRecordingRepository _audioRepo;
    private readonly ILessonMediaStorage _mediaStorage;
    private readonly IOptions<LessonAssessmentOptions> _options;
    private readonly ILogger<LessonAiGrader> _logger;

    public LessonAiGrader(
        HttpClient http,
        IAudioRecordingRepository audioRepo,
        ILessonMediaStorage mediaStorage,
        IOptions<LessonAssessmentOptions> options,
        ILogger<LessonAiGrader> logger)
    {
        _http = http;
        _audioRepo = audioRepo;
        _mediaStorage = mediaStorage;
        _options = options;
        _logger = logger;
    }

    public async Task<LessonAiGradingOutcome> GradeSubmissionAsync(
        ExerciseType exerciseType,
        string taskInstructions,
        string cefrLevel,
        ScoringRubricEntity rubric,
        string learnerSubmissionPayloadJson,
        decimal maxPoints,
        CancellationToken ct = default)
    {
        string? apiKey = GeminiApiKeyResolver.ResolveKey(null, _options.Value.ApiKey);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Google Gemini API Key is not configured for lesson evaluation.");
        }

        // 1. Extract submission text or audio asset
        var (text, audioGuid) = ExtractSubmissionData(learnerSubmissionPayloadJson);

        string? audioBase64 = null;
        string audioMime = "audio/webm";

        if (audioGuid.HasValue)
        {
            var recording = await _audioRepo.GetByIdAsync(audioGuid.Value, ct);
            if (recording != null && !recording.IsDeleted)
            {
                audioMime = recording.ContentType;
                await using var stream = await _mediaStorage.OpenReadAsync(recording.StorageKey, ct);
                if (stream != null)
                {
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms, ct);
                    if (ms.Length > 0)
                    {
                        audioBase64 = Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
        }

        if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(audioBase64))
        {
            return new LessonAiGradingOutcome(
                IsAssessable: false,
                Score100: 0m,
                PointsEarned: 0m,
                CriteriaBreakdown: new List<LessonAiCriterionScore>(),
                Strengths: new List<string>(),
                Improvements: new List<string> { "Submission was empty or unreadable." },
                SuggestedAnswer: null);
        }

        // 2. Prepare rubric criteria
        var enabledCriteria = rubric.Criteria.Where(c => c.IsEnabled).OrderBy(c => c.DisplayOrder).ToList();
        if (enabledCriteria.Count == 0)
        {
            enabledCriteria = rubric.Criteria.OrderBy(c => c.DisplayOrder).ToList();
        }

        string criteriaDescriptions = string.Join("\n", enabledCriteria.Select(c =>
            $"- Code: '{c.Code}', Name: '{c.Name}', Weight: {c.Weight}%: {c.Description}. Level 0..4 scale."));

        string systemInstruction = $@"You are an authoritative, certified ESL examiner evaluating a learner's exercise submission.
Task Type: {exerciseType}
Task Instructions: {taskInstructions}
Target CEFR Level: {cefrLevel}
Rubric ({rubric.Code} v{rubric.Version}):
{criteriaDescriptions}

Rules:
1. Objectively evaluate the submission against the rubric.
2. For each enabled criterion, assign a raw integer score strictly from 0 to 4 (0=Unacceptable, 1=Weak, 2=Fair/Competent, 3=Good, 4=Excellent).
3. Provide concise, helpful feedback in Vietnamese for each criterion, along with overall strengths and improvements.
4. Return ONLY valid JSON matching this schema:
{{
  ""isAssessable"": true,
  ""criteria"": [
    {{ ""code"": ""CRITERION_CODE"", ""rawScore"": 3, ""feedback"": ""Nhận xét chi tiết..."" }}
  ],
  ""strengths"": [""Điểm làm tốt...""],
  ""improvements"": [""Điểm cần cải thiện...""],
  ""suggestedAnswer"": ""Gợi ý câu trả lời chuẩn (nếu có)...""
}}";

        var parts = new List<object>
        {
            new { text = systemInstruction }
        };

        if (!string.IsNullOrWhiteSpace(text))
        {
            parts.Add(new { text = $"Learner Submitted Text:\n\"{text}\"" });
        }

        if (!string.IsNullOrWhiteSpace(audioBase64))
        {
            parts.Add(new
            {
                inlineData = new
                {
                    mimeType = audioMime,
                    data = audioBase64
                }
            });
        }

        var payload = new
        {
            contents = new[]
            {
                new { parts = parts.ToArray() }
            },
            generationConfig = new
            {
                temperature = 0.2,
                responseMimeType = "application/json"
            }
        };

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Value.ModelCode}:generateContent";

        var (success, statusCode, rawBody) = await GeminiHttpHelper.PostJsonWithRetryAsync(
            _http,
            url,
            payload,
            apiKey,
            _options.Value.MaxRetries,
            _logger,
            ct);

        if (!success)
        {
            _logger.LogWarning("Gemini grading failed with HTTP status {StatusCode}.", (int)statusCode);
            throw new InvalidOperationException($"Lỗi kết nối AI khi chấm bài (mã lỗi {(int)statusCode}).");
        }

        var root = JsonNode.Parse(rawBody);
        string? jsonText = root?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(jsonText))
        {
            throw new InvalidOperationException("AI không trả về kết quả chấm điểm hợp lệ.");
        }

        var parsed = JsonNode.Parse(jsonText);
        if (parsed is not JsonObject parsedObj)
        {
            throw new InvalidOperationException("AI grading response was not a valid JSON object.");
        }

        bool isAssessable = parsedObj["isAssessable"]?.GetValue<bool>() ?? true;
        if (!isAssessable)
        {
            return new LessonAiGradingOutcome(
                IsAssessable: false,
                Score100: 0m,
                PointsEarned: 0m,
                CriteriaBreakdown: new List<LessonAiCriterionScore>(),
                Strengths: new List<string>(),
                Improvements: ReadStringArray(parsedObj["improvements"]),
                SuggestedAnswer: null);
        }

        // 3. Parse and compute authoritative scores
        var criteriaNode = parsedObj["criteria"] as JsonArray;
        var rawScoresMap = new Dictionary<string, (int RawScore, string Feedback)>(StringComparer.OrdinalIgnoreCase);

        if (criteriaNode != null)
        {
            foreach (var item in criteriaNode)
            {
                if (item is JsonObject cObj)
                {
                    string code = cObj["code"]?.ToString() ?? string.Empty;
                    int raw = cObj["rawScore"]?.GetValue<int>() ?? 0;
                    string feedback = cObj["feedback"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        rawScoresMap[code] = (Math.Clamp(raw, 0, 4), feedback);
                    }
                }
            }
        }

        var breakdown = new List<LessonAiCriterionScore>();
        decimal totalWeightedScore = 0m;

        foreach (var c in enabledCriteria)
        {
            int raw = 2; // Default to 2 if missing from model response
            string feedback = $"Đạt yêu cầu đối với tiêu chí {c.Name}.";

            if (rawScoresMap.TryGetValue(c.Code, out var evaluated))
            {
                raw = evaluated.RawScore;
                if (!string.IsNullOrWhiteSpace(evaluated.Feedback))
                {
                    feedback = evaluated.Feedback;
                }
            }

            string descriptor = GetDescriptorForLevel(c, raw);
            decimal component = (raw / 4.0m) * c.Weight;
            totalWeightedScore += component;

            breakdown.Add(new LessonAiCriterionScore(
                CriterionCode: c.Code,
                Name: c.Name,
                Weight: c.Weight,
                RawScore: raw,
                Descriptor: descriptor,
                Feedback: feedback));
        }

        decimal score100 = Math.Round(totalWeightedScore, 2, MidpointRounding.AwayFromZero);
        decimal pointsEarned = Math.Round((score100 / 100.00m) * maxPoints, 2, MidpointRounding.AwayFromZero);

        var strengths = ReadStringArray(parsedObj["strengths"]);
        var improvements = ReadStringArray(parsedObj["improvements"]);
        string? suggestedAnswer = parsedObj["suggestedAnswer"]?.ToString();

        return new LessonAiGradingOutcome(
            IsAssessable: true,
            Score100: score100,
            PointsEarned: pointsEarned,
            CriteriaBreakdown: breakdown,
            Strengths: strengths,
            Improvements: improvements,
            SuggestedAnswer: suggestedAnswer);
    }

    private static (string? Text, Guid? AudioGuid) ExtractSubmissionData(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return (null, null);

        try
        {
            var node = JsonNode.Parse(payloadJson);
            if (node == null) return (null, null);

            Guid? audioGuid = null;
            if (node["audioAssetId"] != null && Guid.TryParse(node["audioAssetId"]!.ToString(), out var g))
            {
                audioGuid = g;
            }

            if (node["writtenText"] != null)
                return (node["writtenText"]!.ToString(), audioGuid);
            if (node["translatedText"] != null)
                return (node["translatedText"]!.ToString(), audioGuid);
            if (node["correctedSentence"] != null)
                return (node["correctedSentence"]!.ToString(), audioGuid);
            if (node["transcribedText"] != null)
                return (node["transcribedText"]!.ToString(), audioGuid);
            if (node["userMessage"] != null)
                return (node["userMessage"]!.ToString(), audioGuid);

            return (node.ToJsonString(), audioGuid);
        }
        catch
        {
            return (payloadJson, null);
        }
    }

    private static string GetDescriptorForLevel(ScoringRubricCriterionEntity c, int level)
    {
        return level switch
        {
            0 => !string.IsNullOrEmpty(c.DescriptorLevel0) ? c.DescriptorLevel0 : "Chưa đạt yêu cầu tối thiểu.",
            1 => !string.IsNullOrEmpty(c.DescriptorLevel1) ? c.DescriptorLevel1 : "Dưới mức trung bình.",
            2 => !string.IsNullOrEmpty(c.DescriptorLevel2) ? c.DescriptorLevel2 : "Đạt mức trung bình.",
            3 => !string.IsNullOrEmpty(c.DescriptorLevel3) ? c.DescriptorLevel3 : "Đạt chuẩn tốt.",
            4 => !string.IsNullOrEmpty(c.DescriptorLevel4) ? c.DescriptorLevel4 : "Xuất sắc.",
            _ => string.Empty
        };
    }

    private static List<string> ReadStringArray(JsonNode? node)
    {
        var list = new List<string>();
        if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                string? s = item?.ToString();
                if (!string.IsNullOrWhiteSpace(s)) list.Add(s.Trim());
            }
        }
        return list;
    }
}
