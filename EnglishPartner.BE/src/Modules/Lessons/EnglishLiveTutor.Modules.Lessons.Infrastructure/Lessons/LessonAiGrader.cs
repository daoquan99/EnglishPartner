using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Domain.Common.Exercises;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Observability;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Infrastructure.Lessons;

public sealed class LessonAiGrader : ILessonAiGrader
{
    private readonly HttpClient _http;
    private readonly IAudioRecordingStore _audioStore;
    private readonly ILessonMediaStorage _mediaStorage;
    private readonly IAgyCliRunner _agyRunner;
    private readonly IOptions<LessonAssessmentOptions> _options;
    private readonly ILogger<LessonAiGrader> _logger;

    // Single constructor: typed HttpClient activation fails when several public constructors are satisfiable.
    public LessonAiGrader(
        HttpClient http,
        IAudioRecordingStore audioStore,
        ILessonMediaStorage mediaStorage,
        IAgyCliRunner agyRunner,
        IOptions<LessonAssessmentOptions> options,
        ILogger<LessonAiGrader> logger)
    {
        _http = http;
        _audioStore = audioStore;
        _mediaStorage = mediaStorage;
        _agyRunner = agyRunner;
        _options = options;
        _logger = logger;
    }

    public async Task<LessonAiGradingOutcome> GradeSubmissionAsync(
        ExerciseType exerciseType,
        string taskInstructions,
        string cefrLevel,
        ScoringRubricSnapshot rubric,
        string learnerSubmissionPayloadJson,
        decimal maxPoints,
        string? taskContextJson = null,
        LessonAiExecutionConfigDto? aiConfig = null,
        CancellationToken ct = default)
    {
        bool isAgy = _agyRunner.IsAgyRequest(aiConfig?.Provider, aiConfig?.AssessmentModelCode);
        if (isAgy)
        {
            _agyRunner.EnsureAgyEnabled();
        }

        string modelCode = !string.IsNullOrWhiteSpace(aiConfig?.AssessmentModelCode)
            ? aiConfig.AssessmentModelCode!
            : (isAgy ? _agyRunner.NormalizeModel(null) : _options.Value.ModelCode);

        string? apiKey = null;
        if (!isAgy)
        {
            apiKey = GeminiApiKeyResolver.ResolveKey(aiConfig?.ApiKey, _options.Value.ApiKey);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Google Gemini API Key is not configured for lesson evaluation.");
            }
        }

        // 1. Extract submission text or audio asset
        var (text, audioGuid) = ExtractSubmissionData(learnerSubmissionPayloadJson);

        string? audioBase64 = null;
        string audioMime = "audio/webm";

        if (audioGuid.HasValue)
        {
            var recording = await _audioStore.GetByIdAsync(audioGuid.Value, ct);
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

        string taskContextSection = !string.IsNullOrWhiteSpace(taskContextJson)
            ? $"\nTask Context and Authoritative References:\n{taskContextJson}\n"
            : "";

        string systemInstruction = $@"You are an authoritative, certified ESL examiner evaluating a learner's exercise submission.
Task Type: {exerciseType}
Task Instructions: {taskInstructions}
Target CEFR Level: {cefrLevel}
{taskContextSection}
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

        string jsonText;

        if (isAgy)
        {
            if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(audioBase64))
            {
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    _logger.LogInformation("Audio evaluation with AGY CLI requested; falling back to Gemini API because CLI is text-only.");
                    isAgy = false;
                }
                else
                {
                    throw new InvalidOperationException("Chấm điểm trực tiếp bằng tệp âm thanh (nói/ghi âm) yêu cầu Google Gemini Cloud API. Vui lòng cấu hình Google API Key hoặc nộp bài tập dạng văn bản.");
                }
            }
        }

        using var call = isAgy ? null : AiTelemetry.Start("lesson.grade", modelCode, ct: ct);

        if (isAgy)
        {
            string fullPrompt = $"{systemInstruction}\n\nLearner Submitted Text:\n\"{text}\"";
            string effectiveAgyModel = _agyRunner.NormalizeModel(modelCode);
            _logger.LogInformation("Grading lesson submission using Antigravity CLI local ({Model})...", effectiveAgyModel);
            jsonText = await _agyRunner.ExecutePromptAsync(fullPrompt, effectiveAgyModel, ct);
        }
        else
        {
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

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelCode}:generateContent";

            bool success;
            System.Net.HttpStatusCode statusCode;
            string rawBody;
            try
            {
                (success, statusCode, rawBody) = await GeminiHttpHelper.PostJsonWithRetryAsync(
                    _http,
                    url,
                    payload,
                    apiKey!,
                    _options.Value.MaxRetries,
                    _logger,
                    ct);
            }
            catch (Exception ex)
            {
                call?.Fail(ex);
                throw;
            }

            if (!success)
            {
                _logger.LogWarning("Gemini grading failed with HTTP status {StatusCode}.", (int)statusCode);
                call?.Fail(statusCode);
                throw new InvalidOperationException($"Lỗi kết nối AI khi chấm bài (mã lỗi {(int)statusCode}).");
            }

            call?.RecordUsage(rawBody);
            var root = JsonNode.Parse(rawBody);
            jsonText = root?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                call?.Fail("invalid_response");
                throw new InvalidOperationException("AI không trả về kết quả chấm điểm hợp lệ.");
            }
        }

        JsonNode? parsed;
        try
        {
            parsed = JsonNode.Parse(jsonText);
        }
        catch (System.Text.Json.JsonException)
        {
            call?.Fail("invalid_response");
            throw;
        }

        if (parsed is not JsonObject parsedObj)
        {
            call?.Fail("invalid_response");
            throw new InvalidOperationException("AI grading response was not a valid JSON object.");
        }

        call?.Succeed();

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
                    if (cObj.TryGetPropertyValue("rawScore", out var rNode) && rNode is JsonValue rVal && rVal.TryGetValue<int>(out var rawInt))
                    {
                        if (rawInt < 0 || rawInt > 4)
                        {
                            throw new InvalidOperationException($"AI criterion score for '{code}' must be between 0 and 4, got {rawInt}.");
                        }
                        string feedback = cObj["feedback"]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(code))
                        {
                            rawScoresMap[code] = (rawInt, feedback);
                        }
                    }
                }
            }
        }

        var breakdown = new List<LessonAiCriterionScore>();
        decimal totalWeightedScore = 0m;

        foreach (var c in enabledCriteria)
        {
            if (!rawScoresMap.TryGetValue(c.Code, out var evaluated))
            {
                throw new InvalidOperationException($"AI response is missing evaluation for criterion '{c.Code}' ({c.Name}).");
            }

            int raw = evaluated.RawScore;
            string feedback = !string.IsNullOrWhiteSpace(evaluated.Feedback)
                ? evaluated.Feedback
                : $"Đạt {raw}/4 cho tiêu chí {c.Name}.";

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
            if (node["spokenText"] != null)
                return (node["spokenText"]!.ToString(), audioGuid);
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

    private static string GetDescriptorForLevel(ScoringRubricCriterionSnapshot c, int level)
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
