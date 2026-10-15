using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Conversations.Entities;

public sealed class ChatMessageEntity : Entity<string>
{
    public string ConversationId { get; private set; } = string.Empty;
    public string TurnId { get; private set; } = string.Empty;
    public string Speaker { get; private set; } = "user";
    public string TranscriptLine1 { get; private set; } = string.Empty;
    public string TranslationLine2 { get; private set; } = string.Empty;
    public bool IsFinal { get; private set; }
    public int SequenceOrder { get; private set; }

    private ChatMessageEntity()
        : base(Guid.NewGuid().ToString("N"))
    {
    }

    public ChatMessageEntity(
        string conversationId,
        string turnId,
        string speaker,
        string transcriptLine1 = "",
        string translationLine2 = "",
        bool isFinal = false,
        int sequenceOrder = 0,
        string? id = null)
        : base(string.IsNullOrWhiteSpace(id) ? $"{conversationId}_{turnId}_{speaker}" : id)
    {
        ConversationId = conversationId;
        TurnId = turnId;
        Speaker = string.IsNullOrWhiteSpace(speaker) ? "user" : speaker.Trim();
        TranscriptLine1 = transcriptLine1 ?? string.Empty;
        TranslationLine2 = translationLine2 ?? string.Empty;
        IsFinal = isFinal;
        SequenceOrder = sequenceOrder;
    }

    public static ChatMessageEntity CreateWithTranscript(
        string conversationId,
        string turnId,
        string speaker,
        int sequenceOrder,
        string fullTranscript,
        bool isFinal) =>
        new(conversationId, turnId, speaker, transcriptLine1: fullTranscript, translationLine2: string.Empty, isFinal: isFinal, sequenceOrder: sequenceOrder);

    public static ChatMessageEntity CreateWithTranslation(
        string conversationId,
        string turnId,
        string speaker,
        int sequenceOrder,
        string fullVietnameseText) =>
        new(conversationId, turnId, speaker, transcriptLine1: string.Empty, translationLine2: fullVietnameseText, isFinal: false, sequenceOrder: sequenceOrder);

    public void UpdateTranscript(string fullTranscript, bool isFinal, int sequenceOrder)
    {
        TranscriptLine1 = fullTranscript;
        IsFinal = isFinal || IsFinal;
        if (sequenceOrder > 0 && (SequenceOrder == 0 || sequenceOrder < SequenceOrder))
        {
            SequenceOrder = sequenceOrder;
        }
    }

    public void UpdateTranslation(string fullVietnameseText, int sequenceOrder)
    {
        TranslationLine2 = fullVietnameseText;
        if (sequenceOrder > 0 && SequenceOrder == 0)
        {
            SequenceOrder = sequenceOrder;
        }
    }
}
