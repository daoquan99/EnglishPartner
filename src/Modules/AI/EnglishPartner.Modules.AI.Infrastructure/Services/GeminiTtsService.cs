using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EnglishPartner.Modules.AI.Application.Abstractions;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using EnglishPartner.SharedKernel.Settings;
using EnglishPartner.SharedKernel.Application.Caching;
using Microsoft.Extensions.Options;

namespace EnglishPartner.Modules.AI.Infrastructure.Services;

internal sealed class GeminiTtsService(
    IHttpClientFactory httpClientFactory,
    IOptions<GeminiAISettings> settings,
    IMemoryCache cache,
    ILogger<GeminiTtsService> logger) : IGeminiTtsService
{
    private const string Endpoint = "https://texttospeech.googleapis.com/v1beta1/text:synthesize";
    private GeminiAISettings Config => settings.Value;

    public async Task<byte[]> SynthesizeSpeechAsync(
        string text,
        string? stylePrompt = null,
        string voiceId = "zephyr",
        string? languageCode = null,
        CancellationToken cancellationToken = default)
    {
        var voiceInfo = LiveVoiceCatalog.FindById(voiceId)
                        ?? LiveVoiceCatalog.FindById(LiveVoiceCatalog.DefaultVoice)!;

        var resolvedPrompt = stylePrompt ?? Config.DefaultGeminiTtsStylePrompt;
        var resolvedLanguage = languageCode ?? Config.DefaultTtsLanguage;
        var resolvedModel = Config.DefaultGeminiTtsModel;

        // ── Cache Lookup ──
        var textHash = GetTextHash(text, voiceInfo.Id, resolvedLanguage, resolvedModel, resolvedPrompt);
        var cacheKey = CacheKeys.AI.GeminiTts(textHash, voiceInfo.Id);
        if (cache.TryGetValue(cacheKey, out byte[]? cached) && cached is not null)
        {
            logger.LogDebug("[Gemini TTS] Cache HIT for voice={Voice} key={Key}", voiceInfo.DisplayName, cacheKey[..30]);
            return cached;
        }

        // ── API Call ──
        var requestPayload = new
        {
            audioConfig = new { audioEncoding = "MP3" },
            input = new { text = text, prompt = resolvedPrompt },
            voice = new
            {
                languageCode = resolvedLanguage,
                modelName = resolvedModel,
                name = voiceInfo.DisplayName
            }
        };

        var httpClient = httpClientFactory.CreateClient("GeminiTTS");

        var credential = await GoogleCredential.GetApplicationDefaultAsync().ConfigureAwait(false);
        if (credential.IsCreateScopedRequired)
            credential = credential.CreateScoped("https://www.googleapis.com/auth/cloud-platform");

        string token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync(null, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (!string.IsNullOrEmpty(Config.ProjectId))
            request.Headers.Add("x-goog-user-project", Config.ProjectId);

        request.Content = new StringContent(
            JsonSerializer.Serialize(requestPayload), Encoding.UTF8, "application/json");

        logger.LogInformation("[Gemini TTS] Cache MISS — calling API. voice={Voice} model={Model}",
            voiceInfo.DisplayName, resolvedModel);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("[Gemini TTS] Error from server: {StatusCode} {Error}", response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("audioContent", out var audioContentElement))
            throw new InvalidOperationException("Failed to extract audioContent from Google TTS response.");

        var audioBytes = Convert.FromBase64String(audioContentElement.GetString() ?? string.Empty);

        // ── Cache (Low priority — evicted first under memory pressure) ──
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheTtl.AI.TtsAudio)
            .SetSize(audioBytes.Length)
            .SetPriority(CacheItemPriority.Low);
        cache.Set(cacheKey, audioBytes, cacheOptions);

        logger.LogDebug("[Gemini TTS] Cached {Bytes} bytes for key={Key}", audioBytes.Length, cacheKey[..30]);
        return audioBytes;
    }

    /// <summary>Builds a stable SHA256 hash from all TTS parameters.</summary>
    private static string GetTextHash(string text, string voiceId, string language, string model, string prompt)
    {
        var raw = $"{model}|{voiceId}|{language}|{prompt}|{text}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
