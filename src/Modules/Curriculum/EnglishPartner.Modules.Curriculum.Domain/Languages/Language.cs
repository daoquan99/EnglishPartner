using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Curriculum.Domain.Languages;

/// <summary>
/// Represents a supported language in the system.
/// Seeded with default languages (English, Vietnamese, etc.).
/// </summary>
public sealed class Language : AggregateRoot
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string NativeName { get; private set; } = null!;
    public int SortOrder { get; private set; }

    private Language() { }

    public static Language Create(string code, string name, string nativeName, int sortOrder = 0)
    {
        return new Language
        {
            Id = Guid.NewGuid(),
            Code = code.ToLowerInvariant(),
            Name = name,
            NativeName = nativeName,
            SortOrder = sortOrder
        };
    }

    public void Update(string name, string nativeName, int sortOrder)
    {
        Name = name;
        NativeName = nativeName;
        SortOrder = sortOrder;
    }
}
