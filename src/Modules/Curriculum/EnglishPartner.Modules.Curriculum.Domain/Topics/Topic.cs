using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Curriculum.Domain.Topics;

/// <summary>
/// A learning topic that groups related vocabulary and grammar content.
/// Examples: "Greetings", "Food & Drink", "Travel", "Business English".
/// </summary>
public sealed class Topic : AggregateRoot
{
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string LanguageCode { get; private set; } = "en";
    public string? Level { get; private set; }
    public string? ImageUrl { get; private set; }
    public int SortOrder { get; private set; }

    private Topic() { }

    public static Topic Create(
        string title,
        string languageCode,
        string? description = null,
        string? level = null,
        string? imageUrl = null,
        int sortOrder = 0)
    {
        var topic = new Topic
        {
            Id = Guid.NewGuid(),
            Title = title,
            LanguageCode = languageCode.ToLowerInvariant(),
            Description = description,
            Level = level,
            ImageUrl = imageUrl,
            SortOrder = sortOrder
        };

        topic.RaiseDomainEvent(new Events.TopicCreatedDomainEvent(topic.Id));
        return topic;
    }

    public void Update(string title, string? description, string? level, string? imageUrl, int sortOrder)
    {
        Title = title;
        Description = description;
        Level = level;
        ImageUrl = imageUrl;
        SortOrder = sortOrder;
    }
}
