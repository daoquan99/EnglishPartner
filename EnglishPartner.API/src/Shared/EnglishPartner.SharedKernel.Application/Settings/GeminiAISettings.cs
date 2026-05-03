namespace EnglishPartner.SharedKernel.Settings;

public sealed class GeminiAISettings
{
    public const string SectionName = "GeminiAI";

    // ── Auth ────────────────────────────────────────────────────────────────
    /// <summary>API key from Google Cloud Console — used as x-goog-api-key header.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public string LiveApiKey { get; set; } = string.Empty;

    /// <summary>Google Cloud Project ID. Required for Google.Cloud.AIPlatform.V1 SDK.</summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Google Cloud Region (e.g. us-central1). Required for Google.Cloud.AIPlatform.V1 SDK.</summary>
    public string Location { get; set; } = "us-central1";

    // ── Model ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Model name for Vertex AI Express Mode.
    /// Examples: "gemini-2.5-flash-lite", "gemini-2.0-flash", "gemini-1.5-pro"
    /// Full list: https://cloud.google.com/vertex-ai/generative-ai/docs/learn/models
    /// </summary>
    public string ModelName { get; set; } = "gemini-2.0-flash";

    /// <summary>
    /// Model name used expressly for Live Audio sessions (e.g. "gemini-live-2.5-flash-native-audio" or "gemini-2.0-flash-exp").
    /// </summary>
    public string LiveModelName { get; set; } = "gemini-live-2.5-flash-native-audio";

    /// <summary>
    /// Speaker voice for the audio output (e.g., Aoede, Charon, Kore, Fenrir, Puck, Zephyr).
    /// </summary>
    public string VoiceName { get; set; } = "zephyr";

    // ── Generation Config ───────────────────────────────────────────────────
    public int MaxOutputTokens { get; set; } = 8192;
    public double Temperature { get; set; } = 0.7;
    public double TopP { get; set; } = 0.95;

    /// <summary>E.g. "HIGH", "LOW", or null to disable thinking mode.</summary>
    public string? ThinkingLevel { get; set; }

    /// <summary>If true, disables all HARM safety categories (OFF).</summary>
    public bool DisableAllSafetyRestrictions { get; set; } = false;

    /// <summary>
    /// Timeout (seconds) for a single Gemini API call.
    /// Content generation may need 60–120s. Default: 120s.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;

    // ── Text-to-Speech defaults ─────────────────────────────────────────────
    /// <summary>
    /// Default TTS voice name.
    /// See: https://cloud.google.com/text-to-speech/docs/voices
    /// Examples: "en-US-Neural2-D" (male), "en-US-Neural2-F" (female)
    /// </summary>
    public string DefaultTtsVoice { get; set; } = "en-US-Neural2-C";

    /// <summary>BCP-47 language code for TTS (e.g. "en-US", "vi-VN").</summary>
    public string DefaultTtsLanguage { get; set; } = "en-US";

    /// <summary>Default speaking rate (0.25–4.0). 1.0 = normal speed.</summary>
    public double DefaultTtsSpeakingRate { get; set; } = 1.0;

    /// <summary>
    /// Default model for Gemini TTS (e.g. "gemini-2.5-flash-tts" or "gemini-3.1-flash-tts-preview").
    /// </summary>
    public string DefaultGeminiTtsModel { get; set; } = "gemini-2.5-flash-tts";

    /// <summary>
    /// Default prompt instructing the Gemini API how to read for Gemini TTS.
    /// </summary>
    public string DefaultGeminiTtsStylePrompt { get; set; } = "Read aloud in a clear, encouraging, and highly professional tone of a native English language teacher.";

    // ── Speech-to-Text defaults ─────────────────────────────────────────────
    /// <summary>BCP-47 language code for STT recognition (e.g. "en-US").</summary>
    public string DefaultSttLanguage { get; set; } = "en-US";

    // ── Chat defaults ───────────────────────────────────────────────────────

    /// <summary>
    /// Maximum number of recent messages to include in ChatAsync history.
    /// Older messages are dropped to control token usage and latency.
    /// Default: 20 messages (~10 turns).
    /// </summary>
    public int MaxChatHistoryMessages { get; set; } = 20;

    public string DefaultChatSystemPrompt { get; set; } = "You are a friendly, encouraging, and highly effective English language tutor.\nYou are chatting with a student whose current English level is: {level}.{topicInstruction}\n\nGuidelines:\n1. Always adjust your vocabulary and grammar complexity to match a {level} student.\n2. If the student makes a mistake, gently correct them, explain why, and encourage them.\n3. Keep your responses concise and engaging (1-3 short paragraphs).\n4. Ask questions to keep the conversation flowing.\n5. Do not write essays or overwhelm the student with too much information at once.";

    // ── Computed ────────────────────────────────────────────────────────────
    /// <summary>
    /// Vertex AI Express Mode endpoint — no project ID or region required.
    /// https://aiplatform.googleapis.com/v1/publishers/google/models/{model}:generateContent
    /// </summary>
    public string BuildApiUrl() =>
        $"https://aiplatform.googleapis.com/v1/publishers/google/models/{ModelName}:generateContent";
}
