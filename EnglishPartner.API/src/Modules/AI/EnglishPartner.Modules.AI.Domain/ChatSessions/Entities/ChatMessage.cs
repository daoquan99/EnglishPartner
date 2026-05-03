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
    public int Order { get; private set; }
    public string? AudioUrl { get; private set; }
    public string? Translation { get; private set; }

    private ChatMessage() { }

    internal static ChatMessage Create(Guid sessionId, ChatRole role, string text, int order)
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = role,
            Text = text,
            Order = order
            // CreatedAt is auto-set by AuditableEntityInterceptor
        };
    }

    internal void UpdateAudioUrl(string audioUrl)
    {
        AudioUrl = audioUrl;
    }

    internal void UpdateTranslation(string translation)
    {
        Translation = translation;
    }
}
