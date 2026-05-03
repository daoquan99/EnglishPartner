using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Curriculum.Domain.Conversations;

/// <summary>
/// A simulated conversation or dialogue used for learning.
/// </summary>
public sealed class Conversation : AggregateRoot
{
    public string Title { get; private set; } = null!;
    public string? Scenario { get; private set; }
    public string LanguageCode { get; private set; } = "en";
    public string? Level { get; private set; }
    public string? ImageUrl { get; private set; }
    public Guid? TopicId { get; private set; }
    public Guid? GrammarId { get; private set; }
    /// <summary>True when this entry was created by AI generation, not manually entered.</summary>
    public bool IsAiGenerated { get; private set; }

    private readonly List<Entities.ConversationLine> _lines = [];
    public IReadOnlyList<Entities.ConversationLine> Lines => _lines.AsReadOnly();

    private Conversation() { }

    public static Conversation Create(
        string title,
        string languageCode,
        string? scenario = null,
        string? level = null,
        string? imageUrl = null,
        Guid? topicId = null,
        Guid? grammarId = null,
        bool isAiGenerated = false)
    {
        return new Conversation
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            LanguageCode = languageCode.ToLowerInvariant(),
            Scenario = scenario?.Trim(),
            Level = level,
            ImageUrl = imageUrl,
            TopicId = topicId,
            GrammarId = grammarId,
            IsAiGenerated = isAiGenerated
        };
    }

    public void Update(
        string title,
        string? scenario,
        string? level,
        string? imageUrl,
        Guid? topicId,
        Guid? grammarId)
    {
        Title = title.Trim();
        Scenario = scenario?.Trim();
        Level = level;
        ImageUrl = imageUrl;
        TopicId = topicId;
        GrammarId = grammarId;
    }

    /// <summary>
    /// Adds a new line to the conversation. Use line.AddTranslation() to attach translations.
    /// </summary>
    public Entities.ConversationLine AddLine(string speaker, string text, string? audioUrl)
    {
        var line = Entities.ConversationLine.Create(Id, speaker, text, audioUrl, _lines.Count);
        _lines.Add(line);
        return line;
    }

    public Entities.ConversationLine? UpdateLine(Guid lineId, string speaker, string text, string? audioUrl)
    {
        var line = _lines.FirstOrDefault(l => l.Id == lineId);
        if (line is null) return null;

        line.Update(speaker, text, audioUrl);
        return line;
    }

    public void ClearLines() => _lines.Clear();

    /// <summary>
    /// Reorders lines by the provided ordered list of line IDs.
    /// </summary>
    public void ReorderLines(IReadOnlyList<Guid> orderedLineIds)
    {
        var ids = orderedLineIds.ToList();
        foreach (var line in _lines)
        {
            var index = ids.IndexOf(line.Id);
            if (index >= 0)
                line.SetSortOrder(index);
        }
    }
}
