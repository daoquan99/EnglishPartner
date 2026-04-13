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
        Guid? grammarId = null)
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
            GrammarId = grammarId
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

    public void AddLine(string speaker, string text, string? translation, string? audioUrl)
    {
        _lines.Add(Entities.ConversationLine.Create(Id, speaker, text, translation, audioUrl, _lines.Count));
    }

    public void ClearLines() => _lines.Clear();
}
