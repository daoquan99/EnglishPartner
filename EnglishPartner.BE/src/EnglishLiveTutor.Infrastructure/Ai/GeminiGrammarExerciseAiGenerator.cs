using System.Net.Http.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Ports;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Ai;

public sealed class GeminiGrammarExerciseAiGenerator : IGrammarExerciseAiGenerator
{
    private readonly HttpClient _http;
    private readonly IAppDbContextFactory _dbFactory;
    private readonly IAgyCliRunner? _agyRunner;

    public GeminiGrammarExerciseAiGenerator(
        HttpClient http,
        IAppDbContextFactory dbFactory,
        IAgyCliRunner? agyRunner = null)
    {
        _http = http;
        _dbFactory = dbFactory;
        _agyRunner = agyRunner;
    }

    public async Task<string> GenerateRawAsync(string prompt, GrammarAiExecutionConfig config, CancellationToken ct = default)
    {
        string requestedModelCode = string.IsNullOrWhiteSpace(config.ModelCode) ? "gemini-3.8-flash" : config.ModelCode.Trim();

        bool isAgy = string.Equals(config.Provider, "agy", StringComparison.OrdinalIgnoreCase) ||
                     requestedModelCode.StartsWith("agy:", StringComparison.OrdinalIgnoreCase) ||
                     requestedModelCode.EndsWith("-medium", StringComparison.OrdinalIgnoreCase) ||
                     requestedModelCode.EndsWith("-low", StringComparison.OrdinalIgnoreCase) ||
                     requestedModelCode.EndsWith("-high", StringComparison.OrdinalIgnoreCase) ||
                     (_agyRunner?.IsAgyModel(requestedModelCode) ?? false);

        if (isAgy)
        {
            if (_agyRunner == null)
            {
                throw new InvalidOperationException("Antigravity CLI runner is not configured.");
            }

            string effectiveAgyModel = _agyRunner.NormalizeModel(requestedModelCode);
            return await _agyRunner.ExecutePromptAsync(prompt, effectiveAgyModel, ct);
        }

        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new InvalidOperationException("Cần cung cấp API Key của Google Gemini hoặc chọn Antigravity CLI Local để sinh bài tập AI.");
        }

        string effectiveModel = requestedModelCode;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var model = await db.AiModelCatalog.AsNoTracking()
                .FirstOrDefaultAsync(m => m.ModelCode == requestedModelCode, ct);

            if (model != null)
            {
                if (!model.IsEnabled || !model.IsAvailable || !model.SupportsTextGeneration || model.SupportsLiveAudio)
                {
                    throw new InvalidOperationException($"Model '{model.DisplayName}' không hỗ trợ tạo bài tập ngữ pháp JSON.");
                }
                effectiveModel = model.GetEffectiveModelCode();
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch
        {
            // If db catalog lookup fails, fall back to requestedModelCode directly
            effectiveModel = requestedModelCode;
        }

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(effectiveModel)}:generateContent";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("x-goog-api-key", config.ApiKey.Trim());

        var payload = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new
            {
                temperature = 0.4,
                responseMimeType = "application/json"
            }
        };

        request.Content = JsonContent.Create(payload);

        HttpResponseMessage resp;
        try
        {
            resp = await _http.SendAsync(request, ct);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new InvalidOperationException("grammar_ai_timeout");
        }

        using var _ = resp;
        string rawBody = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            // Do not surface the provider body: it is upstream detail and may echo request data.
            throw new InvalidOperationException($"grammar_ai_provider_error_{(int)resp.StatusCode}");
        }

        var root = JsonNode.Parse(rawBody);
        var contentNode = root?["candidates"]?[0]?["content"];
        string? jsonText = null;
        if (contentNode is JsonObject contentObj)
        {
            jsonText = contentObj["parts"]?[0]?["text"]?.GetValue<string>()
                       ?? contentObj["text"]?.GetValue<string>();
        }
        else if (contentNode is JsonArray contentArr)
        {
            jsonText = contentArr[0]?["text"]?.GetValue<string>()
                       ?? contentArr[0]?["parts"]?[0]?["text"]?.GetValue<string>();
        }

        if (string.IsNullOrWhiteSpace(jsonText))
        {
            throw new InvalidOperationException("AI không trả về dữ liệu JSON hợp lệ.");
        }

        return jsonText;
    }
}
