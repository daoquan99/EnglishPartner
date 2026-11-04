using System.Net.Http.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.AiModels.Contracts;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Ports;
using EnglishLiveTutor.Infrastructure.Observability;

namespace EnglishLiveTutor.Infrastructure.Ai;

public sealed class GeminiGrammarExerciseAiGenerator : IGrammarExerciseAiGenerator
{
    private readonly HttpClient _http;
    private readonly IAiModelCatalogReader _modelCatalogReader;
    private readonly IAgyCliRunner? _agyRunner;

    public GeminiGrammarExerciseAiGenerator(
        HttpClient http,
        IAiModelCatalogReader modelCatalogReader,
        IAgyCliRunner? agyRunner = null)
    {
        _http = http;
        _modelCatalogReader = modelCatalogReader;
        _agyRunner = agyRunner;
    }

    public async Task<string> GenerateRawAsync(string prompt, GrammarAiExecutionConfig config, CancellationToken ct = default)
    {
        string requestedModelCode = string.IsNullOrWhiteSpace(config.ModelCode) ? "gemini-3.8-flash" : config.ModelCode.Trim();

        bool isAgy = _agyRunner.IsAgyRequest(config.Provider, requestedModelCode);

        if (isAgy)
        {
            _agyRunner.EnsureAgyEnabled();
            string effectiveAgyModel = _agyRunner!.NormalizeModel(requestedModelCode);
            return await _agyRunner.ExecutePromptAsync(prompt, effectiveAgyModel, ct);
        }

        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new InvalidOperationException("Cần cung cấp API Key của Google Gemini hoặc chọn Antigravity CLI Local để sinh bài tập AI.");
        }

        string effectiveModel = requestedModelCode;
        try
        {
            var model = await _modelCatalogReader.FindByModelCodeAsync(requestedModelCode, ct);

            if (model != null)
            {
                if (!model.IsEnabled || !model.IsAvailable || !model.SupportsTextGeneration || model.SupportsLiveAudio)
                {
                    throw new InvalidOperationException($"Model '{model.DisplayName}' không hỗ trợ tạo bài tập ngữ pháp JSON.");
                }
                effectiveModel = model.EffectiveModelCode;
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

        using var call = AiTelemetry.Start("grammar.generate", effectiveModel, ct: ct);
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
            call.Fail("timeout");
            throw new InvalidOperationException("grammar_ai_timeout");
        }
        catch (Exception ex)
        {
            call.Fail(ex);
            throw;
        }

        using var _ = resp;
        string rawBody = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            call.Fail(resp.StatusCode);
            // Do not surface the provider body: it is upstream detail and may echo request data.
            throw new InvalidOperationException($"grammar_ai_provider_error_{(int)resp.StatusCode}");
        }

        call.RecordUsage(rawBody);
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
            call.Fail("invalid_response");
            throw new InvalidOperationException("AI không trả về dữ liệu JSON hợp lệ.");
        }

        call.Succeed();
        return jsonText;
    }
}
