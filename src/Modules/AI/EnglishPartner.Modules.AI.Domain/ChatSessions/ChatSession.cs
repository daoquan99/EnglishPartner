using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.AI.Domain.ChatSessions;

/// <summary>
/// Represents a conversation session between a user and the AI Tutor.
/// </summary>
public sealed class ChatSession : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string SystemPrompt { get; private set; } = string.Empty;

    private List<ChatMessage> _messages = [];
    public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();

    private ChatSession() { }

    public static ChatSession Start(Guid userId, string title, string systemPrompt)
    {
        return new ChatSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            SystemPrompt = systemPrompt
        };
    }

    public ChatMessage AddUserMessage(string text)
    {
        var message = ChatMessage.Create(Id, ChatRole.User, text);
        _messages.Add(message);
        return message;
    }

    public ChatMessage AddModelMessage(string text)
    {
        var message = ChatMessage.Create(Id, ChatRole.Model, text);
        _messages.Add(message);
        return message;
    }
}

public enum ChatRole
{
    User = 0,
    Model = 1
}
