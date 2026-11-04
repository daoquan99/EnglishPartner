namespace EnglishLiveTutor.Application.Common.Models;

public sealed record BuildSystemPromptPreviewRequest(
    string CefrLevel = "B1",
    bool ExtendedThinking = false,
    string VoiceName = "Aoede",
    double SpeechSpeed = 1.0,
    string ConversationMode = "daily_conversation",
    string Topic = "",
    string? CustomSystemPrompt = null
);
