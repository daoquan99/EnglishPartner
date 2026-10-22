using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Infrastructure.Ai;

public sealed class GeminiRolePlayReplyProvider : IRolePlayReplyProvider
{
    private readonly HttpClient _http;
    private readonly IOptions<LessonRolePlayOptions> _options;
    private readonly ILogger<GeminiRolePlayReplyProvider> _logger;

    public GeminiRolePlayReplyProvider(
        HttpClient http,
        IOptions<LessonRolePlayOptions> options,
        ILogger<GeminiRolePlayReplyProvider> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public async Task<string> GenerateReplyAsync(
        string scenario,
        string aiRole,
        IReadOnlyList<RolePlayHistoryItem> conversationHistory,
        int currentTurn,
        int maxTurns,
        CancellationToken ct = default)
    {
        string? apiKey = GeminiApiKeyResolver.ResolveKey(null, _options.Value.ApiKey);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Vui lòng cấu hình Google Gemini API Key để sử dụng tính năng Role-Play.");
        }

        string systemInstruction = $"You are role-playing as '{aiRole}' in an ESL conversation practice session. " +
            $"Scenario context: '{scenario}'. " +
            $"Current turn: {currentTurn} of {maxTurns}. " +
            "Respond in character, naturally, warmly and concisely in English (1-3 sentences), encouraging the learner to continue the dialogue. " +
            (currentTurn >= maxTurns ? "This is the final turn of the conversation, so conclude the interaction politely." : "");

        var contents = new List<object>();

        foreach (var item in conversationHistory)
        {
            string geminiRole = string.Equals(item.Role, "assistant", StringComparison.OrdinalIgnoreCase) ? "model" : "user";
            contents.Add(new
            {
                role = geminiRole,
                parts = new[] { new { text = item.Content } }
            });
        }

        var payload = new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = systemInstruction } }
            },
            contents = contents.ToArray(),
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 200
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
            _logger.LogWarning("Gemini role-play API call returned status {StatusCode}.", (int)statusCode);
            throw new InvalidOperationException($"Không thể tạo câu trả lời Role-play từ AI (mã lỗi {(int)statusCode}).");
        }

        var root = JsonNode.Parse(rawBody);
        string? reply = root?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(reply))
        {
            throw new InvalidOperationException("AI không trả về nội dung câu thoại role-play hợp lệ.");
        }

        return reply.Trim();
    }
}
