using EnglishLiveTutor.Domain.Conversations.Entities;

namespace EnglishLiveTutor.Application.Features.Prompts.Ports;

public interface ICefrPromptBuilder
{
    Task<string> BuildSystemInstructionAsync(
        string cefrLevel,
        bool extendedThinking,
        string voiceName,
        double speechSpeed,
        string conversationMode = "daily_conversation",
        string topic = "",
        string? customSystemPrompt = null,
        IReadOnlyList<ChatMessageEntity>? previousMessages = null,
        CancellationToken ct = default);
}
