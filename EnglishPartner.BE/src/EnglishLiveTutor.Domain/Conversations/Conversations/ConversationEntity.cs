using EnglishLiveTutor.Domain.Common.Primitives;
using EnglishLiveTutor.Domain.Conversations.Conversations.Entities;

namespace EnglishLiveTutor.Domain.Conversations.Conversations;

public sealed class ConversationEntity : AggregateRoot<string>
{
    private List<ChatMessageEntity> _messages = new();
    private List<LanguageCorrectionEntity> _corrections = new();

    public string Title { get; private set; } = "Buổi luyện nói mới";
    public string CefrLevel { get; private set; } = "B1";
    public bool ExtendedThinking { get; private set; }
    public string VoiceName { get; private set; } = "Aoede";
    public double SpeechSpeed { get; private set; } = 1.0;
    public string ConversationMode { get; private set; } = "daily_conversation";
    public string Topic { get; private set; } = string.Empty;
    public string? CustomSystemPrompt { get; private set; }

    public IReadOnlyCollection<ChatMessageEntity> Messages => _messages.AsReadOnly();
    public IReadOnlyCollection<LanguageCorrectionEntity> Corrections => _corrections.AsReadOnly();

    private ConversationEntity()
        : base(Guid.NewGuid().ToString("N")[..12])
    {
        UpdatedAtUtc = CreatedAtUtc;
    }

    public ConversationEntity(
        string? id,
        string? title,
        string cefrLevel = "B1",
        bool extendedThinking = false,
        string voiceName = "Aoede",
        double speechSpeed = 1.0,
        string conversationMode = "daily_conversation",
        string topic = "",
        string? customSystemPrompt = null,
        DateTimeOffset? nowUtc = null)
        : base(string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N")[..12] : id.Trim())
    {
        string cleanMode = string.IsNullOrWhiteSpace(conversationMode) ? "daily_conversation" : conversationMode.Trim();
        string cleanTopic = (topic ?? string.Empty).Trim();
        string cleanCefr = string.IsNullOrWhiteSpace(cefrLevel) ? "B1" : cefrLevel.Trim();
        var timestampForTitle = (nowUtc ?? DateTimeOffset.UtcNow).ToOffset(TimeSpan.FromHours(7));
        string defaultTitle = !string.IsNullOrWhiteSpace(cleanTopic)
            ? $"{cleanTopic} ({cleanCefr})"
            : $"Hội thoại {cleanCefr} ({timestampForTitle:dd/MM HH:mm})";

        Title = string.IsNullOrWhiteSpace(title) ? defaultTitle : title.Trim();
        CefrLevel = cleanCefr;
        ExtendedThinking = extendedThinking;
        VoiceName = string.IsNullOrWhiteSpace(voiceName) ? "Aoede" : voiceName.Trim();
        SpeechSpeed = speechSpeed <= 0 ? 1.0 : speechSpeed;
        ConversationMode = cleanMode;
        Topic = cleanTopic;
        CustomSystemPrompt = string.IsNullOrWhiteSpace(customSystemPrompt) ? null : customSystemPrompt.Trim();
        UpdatedAtUtc = CreatedAtUtc;
    }

    public static ConversationEntity Create(
        string? id,
        string? title,
        string cefrLevel = "B1",
        bool extendedThinking = false,
        string voiceName = "Aoede",
        double speechSpeed = 1.0,
        string conversationMode = "daily_conversation",
        string topic = "",
        string? customSystemPrompt = null,
        DateTimeOffset? nowUtc = null) =>
        new(id, title, cefrLevel, extendedThinking, voiceName, speechSpeed, conversationMode, topic, customSystemPrompt, nowUtc);

    public void UpdateSettings(
        string? title,
        string cefrLevel,
        bool extendedThinking,
        string voiceName,
        double speechSpeed,
        string conversationMode,
        string topic,
        string? customSystemPrompt,
        DateTimeOffset? nowUtc = null)
    {
        CefrLevel = string.IsNullOrWhiteSpace(cefrLevel) ? "B1" : cefrLevel.Trim();
        ExtendedThinking = extendedThinking;
        VoiceName = string.IsNullOrWhiteSpace(voiceName) ? "Aoede" : voiceName.Trim();
        SpeechSpeed = speechSpeed <= 0 ? 1.0 : speechSpeed;
        ConversationMode = string.IsNullOrWhiteSpace(conversationMode) ? "daily_conversation" : conversationMode.Trim();
        Topic = (topic ?? string.Empty).Trim();
        CustomSystemPrompt = string.IsNullOrWhiteSpace(customSystemPrompt) ? null : customSystemPrompt.Trim();
        UpdatedAtUtc = nowUtc ?? DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(title))
        {
            Title = title.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(Topic) && Title.StartsWith("Hội thoại ", StringComparison.OrdinalIgnoreCase))
        {
            Title = $"{Topic} ({CefrLevel})";
        }
    }

    public void UpdateTitleFromFirstUserMessage(string fullTranscript, DateTimeOffset? nowUtc = null)
    {
        if (string.IsNullOrWhiteSpace(Topic) &&
            Title.StartsWith("Hội thoại ", StringComparison.OrdinalIgnoreCase) &&
            fullTranscript.Length > 6)
        {
            Title = fullTranscript.Length > 42 ? fullTranscript[..42] + "..." : fullTranscript;
            UpdatedAtUtc = nowUtc ?? DateTimeOffset.UtcNow;
        }
    }

    public void Touch(DateTimeOffset? now = null)
    {
        UpdatedAtUtc = now ?? DateTimeOffset.UtcNow;
    }

    public ChatMessageEntity AddTranscriptMessage(
        string turnId,
        string speaker,
        int sequenceOrder,
        string fullTranscript,
        bool isFinal,
        DateTimeOffset? nowUtc = null)
    {
        var message = ChatMessageEntity.CreateWithTranscript(Id, turnId, speaker, sequenceOrder, fullTranscript, isFinal);
        _messages.Add(message);
        UpdatedAtUtc = nowUtc ?? DateTimeOffset.UtcNow;
        return message;
    }

    public LanguageCorrectionEntity AddCorrection(
        string? id,
        string turnId,
        string originalText,
        string correctedText,
        string errorCategory,
        string explanationVi,
        string cefrTip,
        DateTimeOffset? timestamp = null)
    {
        var correction = new LanguageCorrectionEntity(
            id: id,
            conversationId: Id,
            turnId: turnId,
            originalText: originalText,
            correctedText: correctedText,
            errorCategory: errorCategory,
            explanationVi: explanationVi,
            cefrTip: cefrTip,
            createdAtUtc: timestamp);
        _corrections.Add(correction);
        return correction;
    }
}
