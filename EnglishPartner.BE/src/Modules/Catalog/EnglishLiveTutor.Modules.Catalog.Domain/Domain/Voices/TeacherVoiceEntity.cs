using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Voices;

/// <summary>
/// Aggregate Root representing a Google Gemini Live prebuilt Teacher Voice.
/// </summary>
public sealed class TeacherVoiceEntity : AggregateRoot<string>
{
    public string Name { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty; // e.g. "Aoede (Nữ)"
    public string Type { get; private set; } = "prebuilt";
    public string Gender { get; private set; } = "Female"; // Female | Male
    public string Persona { get; private set; } = string.Empty; // Breezy, Upbeat, Informative, Firm...
    public string Style { get; private set; } = string.Empty; // Vietnamese coaching style note
    public string Description { get; private set; } = string.Empty;
    public bool IsFeatured { get; private set; }
    public bool IsEnabled { get; private set; } = true;
    public int DisplayOrder { get; private set; } = 1;

    private TeacherVoiceEntity()
        : base("Aoede")
    {
    }

    public TeacherVoiceEntity(
        string id,
        string name,
        string displayName,
        string type,
        string gender,
        string persona,
        string description,
        string? label = null,
        string? style = null,
        bool isFeatured = false,
        bool isEnabled = true,
        int displayOrder = 1)
        : base(string.IsNullOrWhiteSpace(id) ? throw new InvalidOperationException("Voice Id không được để trống.") : id.Trim())
    {
        Name = string.IsNullOrWhiteSpace(name) ? Id : name.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? Name : displayName.Trim();
        Type = string.IsNullOrWhiteSpace(type) ? "prebuilt" : type.Trim();
        Gender = string.IsNullOrWhiteSpace(gender) ? "Female" : gender.Trim();
        Persona = persona?.Trim() ?? string.Empty;
        Description = description?.Trim() ?? string.Empty;

        string genderVi = string.Equals(Gender, "Male", StringComparison.OrdinalIgnoreCase) ? "Nam" : "Nữ";
        Label = string.IsNullOrWhiteSpace(label) ? $"{DisplayName} ({genderVi})" : label.Trim();
        Style = string.IsNullOrWhiteSpace(style) ? $"{Persona} — {Description}" : style.Trim();
        IsFeatured = isFeatured;
        IsEnabled = isEnabled;
        DisplayOrder = displayOrder <= 0 ? 1 : displayOrder;
    }

    public void UpdateVoice(
        string? displayName,
        string? label,
        string? gender,
        string? persona,
        string? style,
        string? description,
        bool isFeatured,
        bool isEnabled,
        int displayOrder)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
            DisplayName = displayName.Trim();
        if (!string.IsNullOrWhiteSpace(label))
            Label = label.Trim();
        if (!string.IsNullOrWhiteSpace(gender))
            Gender = gender.Trim();
        if (!string.IsNullOrWhiteSpace(persona))
            Persona = persona.Trim();
        if (!string.IsNullOrWhiteSpace(style))
            Style = style.Trim();
        if (!string.IsNullOrWhiteSpace(description))
            Description = description.Trim();
        IsFeatured = isFeatured;
        IsEnabled = isEnabled;
        if (displayOrder > 0)
            DisplayOrder = displayOrder;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
}
