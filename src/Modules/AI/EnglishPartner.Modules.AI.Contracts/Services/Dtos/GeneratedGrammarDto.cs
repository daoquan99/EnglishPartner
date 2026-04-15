namespace EnglishPartner.Modules.AI.Contracts.Services.Dtos;

/// <summary>AI-generated grammar rule ready to be persisted.</summary>
public sealed record GeneratedGrammarDto(
    string Title,
    string? Explanation,
    string? Formula,
    string? Level,
    List<GeneratedGrammarExampleDto> Examples);

public sealed record GeneratedGrammarExampleDto(
    string Sentence,
    string? Note,
    /// <summary>Translations keyed by language code, e.g. {"vi": "...", "ja": "..."}.</summary>
    Dictionary<string, string> Translations);
