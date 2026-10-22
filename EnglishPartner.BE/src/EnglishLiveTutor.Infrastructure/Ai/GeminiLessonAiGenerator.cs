using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using EnglishLiveTutor.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Infrastructure.Ai;

public sealed class GeminiLessonAiGenerator : ILessonAiGenerator
{
    private static readonly Regex MarkdownCodeFenceRegex = new(@"```(?:json)?\s*([\s\S]*?)\s*```", RegexOptions.Compiled);

    private readonly HttpClient _http;
    private readonly IOptions<LessonGenerationOptions> _options;
    private readonly ILogger<GeminiLessonAiGenerator> _logger;

    public GeminiLessonAiGenerator(
        HttpClient http,
        IOptions<LessonGenerationOptions> options,
        ILogger<GeminiLessonAiGenerator> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public async Task<string> GenerateLessonExercisesJsonAsync(
        string promptText,
        string? clientApiKey,
        string? modelCode,
        CancellationToken ct = default)
    {
        string? apiKey = GeminiApiKeyResolver.ResolveKey(clientApiKey, _options.Value.ApiKey);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Vui lòng cấu hình hoặc cung cấp Google Gemini API Key để sinh bài tập AI.");
        }

        string effectiveModel = !string.IsNullOrWhiteSpace(modelCode)
            ? modelCode.Trim()
            : _options.Value.ModelCode;

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{effectiveModel}:generateContent";
        var payload = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = promptText } } }
            },
            generationConfig = new
            {
                temperature = 0.3,
                responseMimeType = "application/json"
            }
        };

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
            _logger.LogWarning("Gemini API returned error {StatusCode} during lesson generation.", (int)statusCode);
            throw new InvalidOperationException($"Google AI Studio trả về mã lỗi {(int)statusCode} khi sinh bài tập.");
        }

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(rawBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Gemini response envelope.");
            throw new InvalidOperationException("Phản hồi từ Google AI Studio không phải JSON hợp lệ.");
        }

        string? jsonText = root?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(jsonText))
        {
            throw new InvalidOperationException("AI không trả về nội dung bài tập hợp lệ.");
        }

        return CleanMarkdownFences(jsonText);
    }

    private static string CleanMarkdownFences(string text)
    {
        var match = MarkdownCodeFenceRegex.Match(text);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }
        return text.Trim();
    }
}
