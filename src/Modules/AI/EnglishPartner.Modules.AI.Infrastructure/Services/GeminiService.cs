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
            return new EvaluationResult(score, parsed.IsCorrect, parsed.SuggestedCorrection ?? "", parsed.Explanation ?? "", parsed.Transcription ?? "");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[Gemini] Failed to parse evaluation response: {Json}", json);
            return new EvaluationResult(0, false, "", "AI trả lời không hợp lệ. Vui lòng thử lại.");
        }
    }

    public async Task<EvaluationResult> EvaluateAudioAsync(string itemContext, byte[] audioData, string mimeType, CancellationToken cancellationToken = default)
    {
        var prompt = $$"""
            You are an English language teacher evaluating a student's spoken audio answer.
            
            Context (question/prompt the student is answering):
            {{itemContext}}
            
            First, carefully transcribe what the student said in the audio.
            Then, evaluate the answer and respond in this exact JSON format (no markdown):
            {
              "transcription": "<exact transcription of the student's audio>",
              "qualityScore": <int 0-5 where 0=empty/no speech, 1=completely wrong/unintelligible, 2=partially wrong/poor pronunciation, 3=correct but with noticeable pronunciation/grammar mistakes, 4=correct with minor issues, 5=perfect native-like>,
              "isCorrect": <bool>,
              "suggestedCorrection": "<the correct version of the answer if they made a mistake>",
              "explanation": "<brief explanation in Vietnamese of what was right/wrong with their pronunciation or grammar>"
            }
            """;

        var base64Audio = Convert.ToBase64String(audioData);
        var requestBody = new
        {
            contents = new[] { 
                new { 
                    role = "user", 
                    parts = new object[] 
                    { 
                        new { text = prompt },
                        new { inline_data = new { mime_type = mimeType, data = base64Audio } }
                    } 
                } 
            },
            generationConfig = BuildGenerationConfig()
        };

        var (json, _, _, _) = await ExecuteGeminiRequestAsync("EvaluateAudio", requestBody, prompt, cancellationToken);

        try
        {
            var parsed = JsonSerializer.Deserialize<EvaluationJsonResponse>(json, JsonOptions);
            if (parsed is null)
                return new EvaluationResult(0, false, "", "Không thể phân tích phản hồi từ AI.");

            var score = Math.Clamp(parsed.QualityScore, 0, 5);
            return new EvaluationResult(score, parsed.IsCorrect, parsed.SuggestedCorrection ?? "", parsed.Explanation ?? "", parsed.Transcription ?? "");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[Gemini] Failed to parse audio evaluation response: {Json}", json);
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
            generationConfig = BuildGenerationConfig(requireJsonResponse: false),
            safetySettings = BuildSafetySettings()
        };

        var sw = Stopwatch.StartNew();
        string responseText;
        bool isSuccess = true;
        string? errorMsg = null;

        try
        {
            logger.LogInformation("[Gemini] Calling Chat on model {Model}", Config.ModelName);
            var url = $"{Config.BaseUrl}/{Config.ModelName}:{Config.GenerateContentApi}";
            if (url.Contains('?') == false && !string.IsNullOrWhiteSpace(Config.ApiKey))
            {
                url += $"?key={Config.ApiKey}";
            }
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

    private object BuildGenerationConfig(bool requireJsonResponse = true)
    {
        var config = new Dictionary<string, object>
        {
            ["maxOutputTokens"] = Config.MaxOutputTokens,
            ["temperature"] = Config.Temperature,
            ["topP"] = Config.TopP
        };

        if (requireJsonResponse)
        {
            config["responseMimeType"] = "application/json";
        }

        if (!string.IsNullOrWhiteSpace(Config.ThinkingLevel))
        {
            config["thinkingConfig"] = new { thinkingLevel = Config.ThinkingLevel };
        }

        return config;
    }

    private object[]? BuildSafetySettings()
    {
        if (!Config.DisableAllSafetyRestrictions)
            return null;

        return new[]
        {
            new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "OFF" },
            new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "OFF" },
            new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "OFF" },
            new { category = "HARM_CATEGORY_HARASSMENT", threshold = "OFF" }
        };
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
            contents = new[] { new { role = "user", parts = new object[] { new { text = prompt } } } },
            generationConfig = BuildGenerationConfig(),
            safetySettings = BuildSafetySettings()
        };

        return await ExecuteGeminiRequestAsync(requestType, requestBody, prompt, cancellationToken);
    }

    private async Task<(string Json, long LatencyMs, bool IsSuccess, string? Error)> ExecuteGeminiRequestAsync(
        string requestType, object requestBody, string promptLog, CancellationToken cancellationToken)
    {
        var url = $"{Config.BaseUrl}/{Config.ModelName}:{Config.GenerateContentApi}";

        // If the BaseUrl doesn't imply an API key header approach, or we want to support both:
        if (url.Contains('?') == false && !string.IsNullOrWhiteSpace(Config.ApiKey))
        {
            url += $"?key={Config.ApiKey}";
        }

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
        var log = AiRequestLog.Create(requestType, promptLog, responseText, Config.ModelName, sw.ElapsedMilliseconds, isSuccess, errorMsg);
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
        public string? Transcription { get; set; }
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
