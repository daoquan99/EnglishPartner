using System.Text.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Infrastructure.Ai;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Infrastructure.Lessons;

public sealed class DictationAiReviewer : IDictationAiReviewer
{
    private readonly HttpClient _http;
    private readonly IOptions<LessonAssessmentOptions> _options;
    private readonly ILogger<DictationAiReviewer> _logger;
    private readonly TimeProvider _timeProvider;

    public DictationAiReviewer(
        HttpClient http,
        IOptions<LessonAssessmentOptions> options,
        ILogger<DictationAiReviewer> logger,
        TimeProvider timeProvider)
    {
        _http = http;
        _options = options;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<DictationAiReviewDto> ReviewAsync(
        Guid answerId,
        string referenceTranscript,
        string learnerTranscript,
        LessonAiExecutionConfigDto? aiConfig = null,
        CancellationToken ct = default)
    {
        string? apiKey = GeminiApiKeyResolver.ResolveKey(aiConfig?.ApiKey, _options.Value.ApiKey);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Google Gemini API Key is not configured for Dictation AI review.");
        }

        string modelCode = !string.IsNullOrWhiteSpace(aiConfig?.AssessmentModelCode)
            ? aiConfig.AssessmentModelCode!
            : _options.Value.ModelCode;

        string systemInstruction = $@"You are an expert English listening & phonetics coach.
A learner completed a Dictation exercise by listening to an English sentence and writing down what they heard.
Authoritative Reference Transcript: ""{referenceTranscript}""
Learner Transcribed Text: ""{learnerTranscript}""

Analyze the learner's transcript compared to the reference transcript.
Identify:
1. Missing words (words present in reference but missed by learner).
2. Extra words (words inserted by learner not in reference).
3. Substituted words (phonetic confusion, e.g. 'hear' instead of 'here', wrong verb endings).
4. Explanations in Vietnamese explaining why the learner might have misheard (e.g. connected speech, weak forms, reduction, homophones, silent letters).
5. 2-3 concise actionable tips/improvements in Vietnamese to improve their listening & transcription skill.

Return ONLY valid JSON matching this schema:
{{
  ""status"": ""Completed"",
  ""feedback"": ""Nhận xét tổng quan bằng tiếng Việt..."",
  ""missingWords"": [""word1""],
  ""extraWords"": [""word2""],
  ""wordSubstitutions"": [
    {{ ""expected"": ""handmade"", ""actual"": ""hand made"" }}
  ],
  ""explanationsVi"": ""Giải thích chi tiết về hiện tượng nối âm, nuốt âm hoặc từ đồng âm..."",
  ""actionableImprovements"": [
    ""Luyện nghe trọng âm từ và hiện tượng lướt âm..."",
    ""Chú ý đuôi thì quá khứ -ed...""
  ]
}}";

        var payload = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = systemInstruction } } }
            },
            generationConfig = new
            {
                temperature = 0.2,
                responseMimeType = "application/json"
            }
        };

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelCode}:generateContent";

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
            throw new InvalidOperationException($"Lỗi kết nối AI khi nhận xét bài chép chính tả (mã lỗi {(int)statusCode}).");
        }

        var root = JsonNode.Parse(rawBody);
        string? jsonText = root?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(jsonText))
        {
            throw new InvalidOperationException("AI không trả về kết quả nhận xét Dictation hợp lệ.");
        }

        var parsed = JsonNode.Parse(jsonText);
        if (parsed is not JsonObject parsedObj)
        {
            throw new InvalidOperationException("AI review response was not a valid JSON object.");
        }

        string feedback = parsedObj["feedback"]?.ToString() ?? "Hoàn thành nhận xét bài chép chính tả.";
        var missingWords = ReadStringList(parsedObj["missingWords"]);
        var extraWords = ReadStringList(parsedObj["extraWords"]);
        var wordSubstitutions = ReadWordSubstitutions(parsedObj["wordSubstitutions"]);
        string explanationsVi = parsedObj["explanationsVi"]?.ToString() ?? string.Empty;
        var actionableImprovements = ReadStringList(parsedObj["actionableImprovements"]);

        return new DictationAiReviewDto(
            ReviewId: Guid.NewGuid(),
            AnswerId: answerId,
            Status: "Completed",
            Feedback: feedback,
            MissingWords: missingWords,
            ExtraWords: extraWords,
            WordSubstitutions: wordSubstitutions,
            ExplanationsVi: explanationsVi,
            ActionableImprovements: actionableImprovements,
            ErrorMessage: null,
            ReviewedAtUtc: _timeProvider.GetUtcNow());
    }

    private static List<string> ReadStringList(JsonNode? node)
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

    private static List<DictationWordSubstitutionDto> ReadWordSubstitutions(JsonNode? node)
    {
        var list = new List<DictationWordSubstitutionDto>();
        if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item is JsonObject obj)
                {
                    string expected = obj["expected"]?.ToString() ?? string.Empty;
                    string actual = obj["actual"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(expected) || !string.IsNullOrWhiteSpace(actual))
                    {
                        list.Add(new DictationWordSubstitutionDto(expected, actual));
                    }
                }
            }
        }
        return list;
    }
}
