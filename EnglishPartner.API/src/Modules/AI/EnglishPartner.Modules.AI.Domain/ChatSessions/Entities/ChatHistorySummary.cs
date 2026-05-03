using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.AI.Domain.ChatSessions;

/// <summary>
/// A progressive summary of a batch of chat messages.
/// Used to compress older conversation context before sending to the AI model.
/// </summary>
public sealed class ChatHistorySummary : Entity
{
    public Guid SessionId { get; private set; }

    /// <summary>First message Order included in this summary.</summary>
    public int StartOrder { get; private set; }

    /// <summary>Last message Order included in this summary.</summary>
    public int EndOrder { get; private set; }

    /// <summary>AI-generated summary text.</summary>
    public string Summary { get; private set; } = string.Empty;

    private ChatHistorySummary() { }

    internal static ChatHistorySummary Create(Guid sessionId, int startOrder, int endOrder, string summary)
    {
        return new ChatHistorySummary
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            StartOrder = startOrder,
            EndOrder = endOrder,
            Summary = summary
        };
    }
}
