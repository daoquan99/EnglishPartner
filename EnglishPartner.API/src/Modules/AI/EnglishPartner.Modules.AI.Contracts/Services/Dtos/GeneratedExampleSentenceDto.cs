namespace EnglishPartner.Modules.AI.Contracts.Services.Dtos;

/// <summary>A single AI-generated example sentence with translations for multiple target languages.</summary>
public sealed record GeneratedExampleSentenceDto(
    string Text,
    string? Note,
    /// <summary>Translations keyed by language code, e.g. {"vi": "...", "ja": "..."}.</summary>
    Dictionary<string, string> Translations);
