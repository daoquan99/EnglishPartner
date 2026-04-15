namespace EnglishPartner.Modules.AI.Contracts.Services.Dtos;

/// <summary>AI-generated conversation script ready to be persisted.</summary>
public sealed record GeneratedConversationContentDto(
    string Title,
    List<GeneratedConversationLineDto> Lines);

public sealed record GeneratedConversationLineDto(
    string Speaker,
    string Text,
    /// <summary>Translations keyed by language code, e.g. {"vi": "...", "ja": "..."}.</summary>
    Dictionary<string, string> Translations);
