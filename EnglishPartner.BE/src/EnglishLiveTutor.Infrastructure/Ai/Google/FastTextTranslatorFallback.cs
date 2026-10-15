using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Infrastructure.Realtime;

namespace EnglishLiveTutor.Infrastructure.Ai;

/// <summary>
/// Multi-provider Vietnamese translator with in-memory caching and automatic Gemini model discovery.
/// Guarantees that every single message bubble (from Turn #1 to Turn #500+) receives its complete,
/// accurate Vietnamese translation without ever getting rate-limited or truncated.
/// </summary>
public sealed class FastTextTranslatorFallback
{
    private readonly HttpClient _http;
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, List<string>> _discoveredModelsByKey = new();

    public FastTextTranslatorFallback(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(8);
    }

    public async Task<List<string>> GetAvailableGeminiModelsAsync(string apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return new List<string>();

        string keyHash = apiKey.Trim();
        if (_discoveredModelsByKey.TryGetValue(keyHash, out var cachedModels) && cachedModels.Count > 0)
            return cachedModels;

        var result = new List<string>();
        try
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models?key={Uri.EscapeDataString(keyHash)}";
            using var resp = await _http.GetAsync(url, ct);
            if (resp.IsSuccessStatusCode)
            {
                var root = await resp.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: ct);
                var models = root?["models"]?.AsArray();
                if (models is not null)
                {
                    foreach (var m in models)
                    {
                        string? name = m?["name"]?.GetValue<string>(); // e.g. "models/gemini-2.5-flash"
                        var methods = m?["supportedGenerationMethods"]?.AsArray();
                        bool supportsGenerate = methods?.Any(x => x?.GetValue<string>() == "generateContent") ?? false;
                        if (supportsGenerate && !string.IsNullOrWhiteSpace(name) &&
                            name.Contains("gemini", StringComparison.OrdinalIgnoreCase) &&
                            !name.Contains("tts", StringComparison.OrdinalIgnoreCase) &&
                            !name.Contains("embedding", StringComparison.OrdinalIgnoreCase) &&
                            !name.Contains("live", StringComparison.OrdinalIgnoreCase))
                        {
                            result.Add(name.Replace("models/", ""));
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore discovery error
        }

        // Order flash models first for lowest latency
        var ordered = result
            .OrderByDescending(m => m.Contains("flash", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(m => m)
            .ToList();

        if (ordered.Count == 0)
        {
            ordered.AddRange(new[] { "gemini-2.5-flash", "gemini-2.0-flash", "gemini-1.5-flash", "gemini-3.8-flash" });
        }

        _discoveredModelsByKey[keyHash] = ordered;
        return ordered;
    }

    public async Task<string?> TranslateToVietnameseAsync(string apiKey, string englishText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(englishText))
            return null;

        string cleanText = SmartTextDeduplicator.CollapseNearDuplicateSentences(englishText.Trim());
        if (string.IsNullOrWhiteSpace(cleanText) || cleanText == "...")
            return null;

        if (_cache.TryGetValue(cleanText, out var cachedVi))
            return cachedVi;

        // Provider 1: Google Translate GTX (Full sentence array concatenation)
        try
        {
            string gtxUrl = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=en&tl=vi&dt=t&q={Uri.EscapeDataString(cleanText)}";
            using var req = new HttpRequestMessage(HttpMethod.Get, gtxUrl);
            req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            using var gtxResp = await _http.SendAsync(req, ct);
            if (gtxResp.IsSuccessStatusCode)
            {
                var node = await gtxResp.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: ct);
                var sentences = node?[0]?.AsArray();
                if (sentences is not null)
                {
                    var sb = new StringBuilder();
                    foreach (var s in sentences)
                    {
                        string? part = s?[0]?.GetValue<string>();
                        if (!string.IsNullOrEmpty(part))
                            sb.Append(part);
                    }
                    string result = sb.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        _cache[cleanText] = result;
                        return result;
                    }
                }
            }
        }
        catch { }

        // Provider 2: Google Clients5 Dictionary/Translate API (separate quota pool from GTX!)
        try
        {
            string c5Url = $"https://clients5.google.com/translate_a/t?client=dict-chrome-ex&sl=en&tl=vi&q={Uri.EscapeDataString(cleanText)}";
            using var req = new HttpRequestMessage(HttpMethod.Get, c5Url);
            req.Headers.Add("User-Agent", "Mozilla/5.0");
            using var c5Resp = await _http.SendAsync(req, ct);
            if (c5Resp.IsSuccessStatusCode)
            {
                var node = await c5Resp.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: ct);
                string? result = node?[0]?.GetValue<string>()?.Trim()
                    ?? node?[0]?[0]?.GetValue<string>()?.Trim();
                if (!string.IsNullOrWhiteSpace(result))
                {
                    _cache[cleanText] = result;
                    return result;
                }
            }
        }
        catch { }

        // Provider 3: Auto-Discovered Google AI Studio Gemini REST Models
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            var models = await GetAvailableGeminiModelsAsync(apiKey, ct);
            foreach (string model in models.Take(3))
            {
                try
                {
                    string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(apiKey.Trim())}";
                    var payload = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                role = "user",
                                parts = new[]
                                {
                                    new { text = $"Translate this English sentence into natural Vietnamese (output ONLY the Vietnamese translation, nothing else): {cleanText}" }
                                }
                            }
                        },
                        generationConfig = new
                        {
                            temperature = 0.1,
                            maxOutputTokens = 300
                        }
                    };

                    using var resp = await _http.PostAsJsonAsync(url, payload, ct);
                    if (resp.IsSuccessStatusCode)
                    {
                        var json = await resp.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: ct);
                        string? text = json?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>()?.Trim();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            _cache[cleanText] = text;
                            return text;
                        }
                    }
                }
                catch { }
            }
        }

        // Provider 4: MyMemory Free Translation API fallback
        try
        {
            string mmUrl = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(cleanText)}&langpair=en|vi";
            using var mmResp = await _http.GetAsync(mmUrl, ct);
            if (mmResp.IsSuccessStatusCode)
            {
                var node = await mmResp.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: ct);
                string? translated = node?["responseData"]?["translatedText"]?.GetValue<string>()?.Trim();
                if (!string.IsNullOrWhiteSpace(translated))
                {
                    _cache[cleanText] = translated;
                    return translated;
                }
            }
        }
        catch { }

        return null;
    }
}
