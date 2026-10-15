using EnglishLiveTutor.Application.Common.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EnglishLiveTutor.Infrastructure.Ai;

public sealed class GeminiVocabularyAiGenerator : IVocabularyAiGenerator
{
    private readonly HttpClient _http;
    private readonly IVocabularyGenerationContent _content;

    public GeminiVocabularyAiGenerator(HttpClient http, IVocabularyGenerationContent content)
    {
        _http = http;
        _content = content;
    }

    public async Task<List<GeneratedVocabularyRawSense>> GenerateVocabularyAsync(
        string apiKey,
        string modelCode,
        string cefrLevel,
        string topic,
        int count,
        List<string> excludedWords,
        CancellationToken ct = default)
    {
        string prompt = _content.BuildPrompt(cefrLevel, topic, count, excludedWords);

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelCode}:generateContent?key={Uri.EscapeDataString(apiKey.Trim())}";

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


        Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true
        }));

        using var resp = await _http.PostAsJsonAsync(url, payload, ct);
        string rawBody = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Google AI Studio trả về lỗi {(int)resp.StatusCode}: {rawBody}");
        }

        var root = JsonNode.Parse(rawBody);
        string? jsonText = root?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(jsonText))
        {
            throw new InvalidOperationException("AI không trả về dữ liệu JSON hợp lệ.");
        }

        return _content.ParseResponse(jsonText, cefrLevel);
    }
}
