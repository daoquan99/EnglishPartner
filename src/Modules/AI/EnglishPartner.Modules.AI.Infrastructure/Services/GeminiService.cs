using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.Modules.AI.Domain.AiRequestLogs;
using EnglishPartner.Modules.AI.Infrastructure.Persistence;
using EnglishPartner.SharedKernel.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishPartner.Modules.AI.Infrastructure.Services;

/// <summary>
/// Communicates with Google Gemini API via REST.
/// Audit logs are added to DbContext but NOT saved here — UoWBehavior handles persistence.
/// </summary>
internal sealed class GeminiService(
    HttpClient httpClient,
    IOptions<GeminiAISettings> settings,
    AiDbContext dbContext,
    ILogger<GeminiService> logger) : IGeminiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly string GeminiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    private GeminiAISettings Config => settings.Value;

    public async Task<EvaluationResult> EvaluateInputAsync(string itemContext, string userInput, CancellationToken cancellationToken = default)
    {
        var prompt = $$"""
            You are an English language teacher evaluating a student's answer.
            
            Context (question/prompt the student is answering):
            {{itemContext}}
            
            Student's answer:
            {{userInput}}
            
            Evaluate the answer and respond in this exact JSON format (no markdown):
            {
              "qualityScore": <int 0-5 where 0=blackout/no answer, 1=completely wrong, 2=partially wrong, 3=correct but with mistakes, 4=correct with minor issues, 5=perfect>,
              "isCorrect": <bool>,
              "suggestedCorrection": "<the correct version of the answer>",
              "explanation": "<brief explanation in Vietnamese of what was right/wrong>"
            }
            """;

        var (json, _, _, _) = await CallGeminiWithLoggingAsync("Evaluate", prompt, cancellationToken);

        try
        {
            var parsed = JsonSerializer.Deserialize<EvaluationJsonResponse>(json, JsonOptions);
            if (parsed is null)
                return new EvaluationResult(0, false, "", "Không thể phân tích phản hồi từ AI.");

            var score = Math.Clamp(parsed.QualityScore, 0, 5);
            return new EvaluationResult(score, parsed.IsCorrect, parsed.SuggestedCorrection ?? "", parsed.Explanation ?? "");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[Gemini] Failed to parse evaluation response: {Json}", json);
            return new EvaluationResult(0, false, "", "AI trả lời không hợp lệ. Vui lòng thử lại.");
        }
    }

    public async Task<GeneratedConversation> GenerateConversationAsync(string topicName, string level, CancellationToken cancellationToken = default)
    {
        var prompt = $$"""
            Generate an English conversation about the topic "{{topicName}}" at {{level}} level.
            The conversation should have 6-10 turns between 2 speakers (A and B).
            
            Respond in this exact JSON format (no markdown):
            {
              "title": "<conversation title>",
              "lines": [
                {
                  "speaker": "A",
                  "text": "<English text>",
                  "translation": "<Vietnamese translation>"
                }
              ]
            }
            """;

        var (json, _, _, _) = await CallGeminiWithLoggingAsync("GenerateConversation", prompt, cancellationToken);

        try
        {
            var parsed = JsonSerializer.Deserialize<GeneratedConversationJson>(json, JsonOptions);
            if (parsed is null)
                return new GeneratedConversation(topicName, []);

            var lines = parsed.Lines?.Select(l => new ConversationLine(l.Speaker ?? "A", l.Text ?? "", l.Translation ?? "")).ToList() ?? [];
            return new GeneratedConversation(parsed.Title ?? topicName, lines);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[Gemini] Failed to parse conversation response: {Json}", json);
            return new GeneratedConversation(topicName, []);
        }
    }

    public async Task<string> ChatAsync(string systemPrompt, IReadOnlyList<ChatTurn> history, string userMessage, CancellationToken cancellationToken = default)
    {
        // history already contains the user's latest message added by ChatSession.AddUserMessage()
        var requestBody = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = history.Select(h => new
            {
                role = h.Role,
                parts = new[] { new { text = h.Text } }
            }).ToArray(),
            generationConfig = new
            {
                maxOutputTokens = Config.MaxTokens,
                temperature = Config.Temperature
            }
        };

        var sw = Stopwatch.StartNew();
        string responseText;
        bool isSuccess = true;
        string? errorMsg = null;

        try
        {
            logger.LogInformation("[Gemini] Calling Chat on model {Model}", Config.ModelName);
            var url = $"{GeminiBaseUrl}/{Config.ModelName}:generateContent";
            var content = new StringContent(JsonSerializer.Serialize(requestBody, JsonOptions), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(responseJson);
            responseText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";
        }
        catch (Exception ex)
        {
            isSuccess = false;
            errorMsg = ex.Message;
            responseText = "";
            logger.LogError(ex, "[Gemini] Chat failed");
        }
        finally
        {
            sw.Stop();
        }

        // Queue audit log — saved by UoWBehavior, not here
        var log = AiRequestLog.Create("Chat", userMessage, responseText, Config.ModelName, sw.ElapsedMilliseconds, isSuccess, errorMsg);
        dbContext.AiRequestLogs.Add(log);

        logger.LogInformation("[Gemini] Chat completed in {Ms}ms (success={Success})", sw.ElapsedMilliseconds, isSuccess);
        return responseText;
    }

    /// <summary>
    /// Calls Gemini with structured JSON output and queues an audit log entry.
    /// Does NOT call SaveChanges — caller's UoWBehavior handles persistence.
    /// </summary>
    private async Task<(string Json, long LatencyMs, bool IsSuccess, string? Error)> CallGeminiWithLoggingAsync(
        string requestType, string prompt, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
            generationConfig = new
            {
                responseMimeType = "application/json",
                maxOutputTokens = Config.MaxTokens,
                temperature = Config.Temperature
            }
        };

        var url = $"{GeminiBaseUrl}/{Config.ModelName}:generateContent";
        var content = new StringContent(JsonSerializer.Serialize(requestBody, JsonOptions), Encoding.UTF8, "application/json");

        var sw = Stopwatch.StartNew();
        string responseText;
        bool isSuccess = true;
        string? errorMsg = null;

        try
        {
            logger.LogInformation("[Gemini] Calling {RequestType} on model {Model}", requestType, Config.ModelName);
            var response = await httpClient.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(responseJson);
            responseText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "{}";
        }
        catch (Exception ex)
        {
            isSuccess = false;
            errorMsg = ex.Message;
            responseText = "{}";
            logger.LogError(ex, "[Gemini] {RequestType} failed", requestType);
        }
        finally
        {
            sw.Stop();
        }

        // Queue audit log — saved by UoWBehavior at end of command pipeline
        var log = AiRequestLog.Create(requestType, prompt, responseText, Config.ModelName, sw.ElapsedMilliseconds, isSuccess, errorMsg);
        dbContext.AiRequestLogs.Add(log);

        logger.LogInformation("[Gemini] {RequestType} completed in {Ms}ms (success={Success})", requestType, sw.ElapsedMilliseconds, isSuccess);
        return (responseText, sw.ElapsedMilliseconds, isSuccess, errorMsg);
    }

    // ── Internal JSON DTOs ──

    private sealed class EvaluationJsonResponse
    {
        public int QualityScore { get; set; }
        public bool IsCorrect { get; set; }
        public string? SuggestedCorrection { get; set; }
        public string? Explanation { get; set; }
    }

    private sealed class GeneratedConversationJson
    {
        public string? Title { get; set; }
        public List<ConversationLineJson>? Lines { get; set; }
    }

    private sealed class ConversationLineJson
    {
        public string? Speaker { get; set; }
        public string? Text { get; set; }
        public string? Translation { get; set; }
    }
}
