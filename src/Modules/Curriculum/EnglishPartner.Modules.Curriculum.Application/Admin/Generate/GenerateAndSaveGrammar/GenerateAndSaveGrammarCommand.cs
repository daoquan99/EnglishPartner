using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateAndSaveGrammar;

/// <summary>
/// Admin command: Ask AI to generate grammar rules with examples in multiple target languages.
/// </summary>
public sealed record GenerateAndSaveGrammarCommand(
    string Title,
    string? Level = null,
    Guid? TopicId = null,
    int ExampleCount = 3,
    List<string>? TargetLanguageCodes = null,
    int Count = 1) : ICommand<GenerateAndSaveGrammarResponse>;

public sealed record GenerateAndSaveGrammarResponse(
    List<GeneratedGrammarItem> Items);

public sealed record GeneratedGrammarItem(
    Guid Id,
    string Title,
    string? Explanation,
    string? Formula,
    string? Level,
    bool IsAiGenerated,
    List<GeneratedGrammarExampleItem> Examples);

public sealed record GeneratedGrammarExampleItem(
    string Sentence,
    string? Note,
    Dictionary<string, string> Translations);
