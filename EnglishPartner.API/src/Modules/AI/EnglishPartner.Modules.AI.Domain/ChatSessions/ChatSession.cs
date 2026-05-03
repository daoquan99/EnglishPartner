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
    public string? Topic { get; private set; }
    public SessionType SessionType { get; private set; }
    public string? VoiceName { get; private set; }
    public string? VoiceLanguageCode { get; private set; }
    public string? VoiceGender { get; private set; }

    private List<ChatMessage> _messages = [];
    public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();

    private List<ChatHistorySummary> _summaries = [];
    public IReadOnlyList<ChatHistorySummary> Summaries => _summaries.AsReadOnly();

    private ChatSession() { }

    public static ChatSession Start(Guid userId, string title, string systemPrompt, string? topic)
    {
        return new ChatSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            SystemPrompt = systemPrompt,
            Topic = topic,
            SessionType = SessionType.TextChat
        };
    }

    public static ChatSession StartLive(
        Guid userId, string title, string systemPrompt,
        string? voiceName, string? voiceLanguageCode = null, string? voiceGender = null)
    {
        return new ChatSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            SystemPrompt = systemPrompt,
            SessionType = SessionType.LiveAudio,
            VoiceName = voiceName,
            VoiceLanguageCode = voiceLanguageCode,
            VoiceGender = voiceGender
        };
    }

    public ChatMessage AddUserMessage(string text)
    {
        var order = _messages.Count + 1;
        var message = ChatMessage.Create(Id, ChatRole.User, text, order);
        _messages.Add(message);
        return message;
    }

    public ChatMessage AddModelMessage(string text)
    {
        var order = _messages.Count + 1;
        var message = ChatMessage.Create(Id, ChatRole.Model, text, order);
        _messages.Add(message);
        return message;
    }

    public void UpdateMessageAudioUrl(Guid messageId, string audioUrl)
    {
        var message = _messages.FirstOrDefault(m => m.Id == messageId);
        if (message is not null)
        {
            message.UpdateAudioUrl(audioUrl);
        }
    }

    public void UpdateMessageTranslation(Guid messageId, string translation)
    {
        var message = _messages.FirstOrDefault(m => m.Id == messageId);
        if (message is not null)
        {
            message.UpdateTranslation(translation);
        }
    }

    public ChatHistorySummary AddSummary(int startOrder, int endOrder, string summary)
    {
        var entry = ChatHistorySummary.Create(Id, startOrder, endOrder, summary);
        _summaries.Add(entry);
        return entry;
    }
}

public enum ChatRole
{
    User = 0,
    Model = 1
}

public enum SessionType
{
    TextChat = 0,
    LiveAudio = 1
}
