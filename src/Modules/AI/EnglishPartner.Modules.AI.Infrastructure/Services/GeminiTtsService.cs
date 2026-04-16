using System.Net.Http.Headers;
using System.Text.Json;
using EnglishPartner.Modules.AI.Application.Abstractions;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;

using EnglishPartner.SharedKernel.Settings;
using Microsoft.Extensions.Options;

namespace EnglishPartner.Modules.AI.Infrastructure.Services;

internal sealed class GeminiTtsService(
    IHttpClientFactory httpClientFactory,
    IOptions<GeminiAISettings> settings,
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

        // Payload schema based on v1beta1 generic TTS specs + gemini params
        var requestPayload = new
        {
            audioConfig = new
            {
                audioEncoding = "MP3"
            },
            input = new
            {
                text = text,
                prompt = resolvedPrompt
            },
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
        {
            credential = credential.CreateScoped("https://www.googleapis.com/auth/cloud-platform");
        }
        string token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync(null, cancellationToken);
        
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (!string.IsNullOrEmpty(Config.ProjectId))
        {
            request.Headers.Add("x-goog-user-project", Config.ProjectId);
        }
        request.Content = new StringContent(JsonSerializer.Serialize(requestPayload), System.Text.Encoding.UTF8, "application/json");

        logger.LogInformation("[Gemini TTS] Synthesizing '{Voice}' model={Model} prompt='{Prompt}'", voiceInfo.DisplayName, resolvedModel, resolvedPrompt);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("[Gemini TTS] Error from server: {StatusCode} {Error}", response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        
        if (document.RootElement.TryGetProperty("audioContent", out var audioContentElement))
        {
            string base64Audio = audioContentElement.GetString() ?? string.Empty;
            return Convert.FromBase64String(base64Audio);
        }

        throw new InvalidOperationException("Failed to extract audioContent from Google TTS response.");
    }
}
