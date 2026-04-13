using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.AI.Domain.ChatSessions;

/// <summary>
/// A single message in an AI chat conversation.
/// </summary>
public sealed class ChatMessage : Entity
{
    public Guid SessionId { get; private set; }
    public ChatRole Role { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }

    private ChatMessage() { }

    internal static ChatMessage Create(Guid sessionId, ChatRole role, string text)
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = role,
            Text = text,
            Timestamp = DateTime.UtcNow
        };
    }
}
