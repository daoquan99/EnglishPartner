using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Prompts.Contracts;
using EnglishLiveTutor.Domain.Conversations.Entities;

namespace EnglishLiveTutor.Infrastructure.Ai;

/// <summary>
/// Database-backed CEFR & Conversation Mode System Prompt Builder.
/// Loads active CEFR level guidance (CefrLevelPrompts) and Session Mode directives (ConversationModePrompts)
/// from SQL Server while retaining deterministic fallback defaults.
/// </summary>
public sealed class CefrPromptBuilder : ICefrPromptBuilder
{
    private readonly IPromptCatalogReader? _catalogReader;

    public CefrPromptBuilder(IPromptCatalogReader? catalogReader = null)
    {
        _catalogReader = catalogReader;
    }

    public async Task<string> BuildSystemInstructionAsync(
        string cefrLevel,
        bool extendedThinking,
        string voiceName,
        double speechSpeed,
        string conversationMode = "daily_conversation",
        string topic = "",
        string? customSystemPrompt = null,
        IReadOnlyList<ChatMessageEntity>? previousMessages = null,
        CancellationToken ct = default)
    {
        var normalizedLevel = (cefrLevel ?? "B1").Trim().ToUpperInvariant();
        var normalizedMode = (conversationMode ?? "daily_conversation").Trim().ToLowerInvariant();

        string? dbLevelGuidance = null;
        string? dbModeDirective = null;

        if (_catalogReader is not null)
        {
            try
            {
                var snapshot = await _catalogReader.GetActiveAsync(normalizedLevel, normalizedMode, ct);
                dbLevelGuidance = snapshot.LevelGuidance;
                dbModeDirective = snapshot.ModeDirective;
            }
            catch
            {
                // Fallback to built-in defaults if DB is unreachable
            }
        }

        var fallback = _catalogReader?.GetFallback(normalizedLevel, normalizedMode)
            ?? PromptCatalogDefaults.GetFallback(normalizedLevel, normalizedMode);

        return ComposeSystemInstruction(
            normalizedLevel,
            extendedThinking,
            voiceName,
            speechSpeed,
            dbLevelGuidance ?? fallback.LevelGuidance,
            dbModeDirective ?? fallback.ModeDirective,
            topic,
            customSystemPrompt,
            previousMessages);
    }

    public static string BuildSystemInstruction(
        string cefrLevel,
        bool extendedThinking,
        string voiceName,
        double speechSpeed,
        string conversationMode = "daily_conversation",
        string topic = "",
        string? customSystemPrompt = null,
        IReadOnlyList<ChatMessageEntity>? previousMessages = null)
    {
        var normalizedLevel = (cefrLevel ?? "B1").Trim().ToUpperInvariant();
        var normalizedMode = (conversationMode ?? "daily_conversation").Trim().ToLowerInvariant();

        var fallback = PromptCatalogDefaults.GetFallback(normalizedLevel, normalizedMode);

        return ComposeSystemInstruction(
            normalizedLevel,
            extendedThinking,
            voiceName,
            speechSpeed,
            fallback.LevelGuidance,
            fallback.ModeDirective,
            topic,
            customSystemPrompt,
            previousMessages);
    }

    private static string ComposeSystemInstruction(
        string normalizedLevel,
        bool extendedThinking,
        string voiceName,
        double speechSpeed,
        string levelGuidance,
        string modeDirective,
        string topic,
        string? customSystemPrompt,
        IReadOnlyList<ChatMessageEntity>? previousMessages)
    {
        var speedInstruction = speechSpeed switch
        {
            <= 0.8 => "SPEAKING PACE: Speak slowly, articulating every syllable with deliberate pauses between clauses (approx 0.75x normal speed).",
            <= 0.95 => "SPEAKING PACE: Speak at a relaxed, unhurried pace (approx 0.9x normal speed).",
            >= 1.2 => "SPEAKING PACE: Speak briskly and energetically like a fast native speaker (approx 1.25x normal speed).",
            >= 1.1 => "SPEAKING PACE: Speak at a lively, slightly faster-than-average conversational tempo (approx 1.15x normal speed).",
            _ => "SPEAKING PACE: Speak at a natural 1.0x conversational rhythm."
        };

        var thinkingNote = extendedThinking
            ? "Extended Thinking mode is ACTIVE: reason deeply about the learner's ideas and provide rich, well-structured conversational responses without saying filler phrases like 'Let me check on that' or 'Please wait a moment'."
            : "Respond with low-latency conversational warmth and immediate spoken feedback.";

        var lines = new List<string>
        {
            $"You are an expert English-speaking AI Partner & Coach (Voice Persona: {voiceName}) conducting a 1-on-1 live voice session.",
            $"Target Learner CEFR Level: {normalizedLevel}.",
            $"Level Adaptation Rule: {levelGuidance}",
            speedInstruction,
            $"Mode Guidance: {thinkingNote}",
            "",
            "=== SELECTED SESSION MODE ===",
            modeDirective
        };

        if (!string.IsNullOrWhiteSpace(topic))
        {
            lines.Add("");
            lines.Add("=== CONVERSATION TOPIC / SCENARIO ===");
            lines.Add($"Active Topic: \"{topic.Trim()}\"");
            lines.Add("Anchor your questions, examples, vocabulary, and follow-ups directly around this topic.");
        }

        if (!string.IsNullOrWhiteSpace(customSystemPrompt))
        {
            lines.Add("");
            lines.Add("=== USER-DEFINED CUSTOM SYSTEM PROMPT (HIGHEST PRIORITY FOR THIS CONVERSATION) ===");
            lines.Add(customSystemPrompt.Trim());
        }

        lines.Add("");
        lines.Add("=== CORE CONVERSATION RULES ===");
        lines.Add("1. Keep the spoken interaction natural, engaging, and human-like.");
        lines.Add("2. NEVER say robotic filler phrases like 'Let me check on that', 'Please wait a moment', or 'I am looking into ways to...'.");
        lines.Add("3. Do NOT repeat the same sentence twice in a single turn.");
        lines.Add("4. Always speak in English.");

        if (previousMessages is { Count: > 0 })
        {
            lines.Add("");
            lines.Add("=== PREVIOUS CONVERSATION HISTORY TO RESUME SEAMLESSLY ===");
            foreach (var msg in previousMessages.TakeLast(16))
            {
                if (!string.IsNullOrWhiteSpace(msg.TranscriptLine1))
                {
                    string roleLabel = msg.Speaker == "user" ? "User" : "AI (You)";
                    lines.Add($"- {roleLabel}: {msg.TranscriptLine1}");
                }
            }
        }

        return string.Join("\n", lines);
    }

}
