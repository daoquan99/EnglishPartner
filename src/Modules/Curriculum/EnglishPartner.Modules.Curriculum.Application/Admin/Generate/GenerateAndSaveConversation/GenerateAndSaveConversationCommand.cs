using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateAndSaveConversation;

/// <summary>
/// Admin command: Ask AI to generate conversation scripts with translations in multiple languages.
/// </summary>
public sealed record GenerateAndSaveConversationCommand(
    string TargetLanguage,
    string TopicName,
    string? Level = null,
    string? Scenario = null,
    int LineCount = 6,
    List<string>? TargetLanguageCodes = null,
    int Count = 1,
    Guid? TopicId = null,
    Guid? GrammarId = null) : ICommand<GenerateAndSaveConversationResponse>;

public sealed record GenerateAndSaveConversationResponse(
    List<GeneratedConversationItem> Items);

public sealed record GeneratedConversationItem(
    Guid Id,
    string Title,
    bool IsAiGenerated,
    List<GeneratedConversationLineItem> Lines);

public sealed record GeneratedConversationLineItem(
    string Speaker,
    string Text,
    Dictionary<string, string> Translations);
