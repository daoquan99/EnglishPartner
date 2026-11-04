using System.Net.Http.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Infrastructure.Observability;

namespace EnglishLiveTutor.Infrastructure.Ai;

public sealed class GeminiVocabularyAiGenerator : IVocabularyAiGenerator
{
    private readonly HttpClient _http;
    private readonly IVocabularyGenerationContent _content;
    private readonly IAgyCliRunner? _agyRunner;

    public GeminiVocabularyAiGenerator(
        HttpClient http,
        IVocabularyGenerationContent content,
        IAgyCliRunner? agyRunner = null)
    {
        _http = http;
        _content = content;
        _agyRunner = agyRunner;
    }

    public async Task<List<GeneratedVocabularyRawSense>> GenerateVocabularyAsync(
        VocabularyGenerationRequestOptions options,
        CancellationToken ct = default)
    {
        bool isAgy = _agyRunner.IsAgyRequest(options.Provider, options.ModelCode);

        string prompt = _content.BuildPrompt(new VocabularyPromptOptions(
            CefrLevel: options.CefrLevel,
            Topic: options.Topic,
            Count: options.Count,
            ExcludedWords: options.ExcludedWords,
            ExamplesPerSense: options.ExamplesPerSense,
            TargetTerms: options.TargetTerms,
            SourceSentence: options.SourceSentence));

        if (isAgy)
        {
            _agyRunner.EnsureAgyEnabled();
            string effectiveAgyModel = _agyRunner!.NormalizeModel(options.ModelCode);
            string jsonResult = await _agyRunner.ExecutePromptAsync(prompt, effectiveAgyModel, ct);
            return _content.ParseResponse(jsonResult, options.CefrLevel, options.ExamplesPerSense, options.TargetTerms);
        }

        using var call = AiTelemetry.Start("vocabulary.generate", options.ModelCode, ct: ct);
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{options.ModelCode}:generateContent";

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

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("x-goog-api-key", options.ApiKey.Trim());
        request.Content = JsonContent.Create(payload);

        HttpResponseMessage resp;
        try
        {
            resp = await _http.SendAsync(request, ct);
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
            throw new InvalidOperationException($"Google AI Studio trả về lỗi {(int)resp.StatusCode}.");
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
        return _content.ParseResponse(jsonText, options.CefrLevel, options.ExamplesPerSense, options.TargetTerms);
    }
}
