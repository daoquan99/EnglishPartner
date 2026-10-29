using EnglishLiveTutor.Application.Common.Interfaces;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

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
        bool isAgy = string.Equals(options.Provider, "agy", StringComparison.OrdinalIgnoreCase) ||
                     (_agyRunner?.IsAgyModel(options.ModelCode) ?? false);

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
            if (_agyRunner == null)
                throw new InvalidOperationException("Antigravity CLI runner is not configured.");

            string effectiveAgyModel = _agyRunner.NormalizeModel(options.ModelCode);
            string jsonResult = await _agyRunner.ExecutePromptAsync(prompt, effectiveAgyModel, ct);
            return _content.ParseResponse(jsonResult, options.CefrLevel, options.ExamplesPerSense, options.TargetTerms);
        }

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{options.ModelCode}:generateContent?key={Uri.EscapeDataString(options.ApiKey.Trim())}";

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

        using var resp = await _http.PostAsJsonAsync(url, payload, ct);
        string rawBody = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Google AI Studio trả về lỗi {(int)resp.StatusCode}: {rawBody}");
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

        return _content.ParseResponse(jsonText, options.CefrLevel, options.ExamplesPerSense, options.TargetTerms);
    }
}
