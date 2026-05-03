using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateAndSaveExampleSentences;

/// <summary>
/// Admin command: Ask AI to generate N example sentences with translations in multiple languages.
/// </summary>
public sealed record GenerateAndSaveExampleSentencesCommand(
    string TargetLanguage,
    string Context,
    int Count = 3,
    string? Level = null,
    List<string>? TargetLanguageCodes = null,
    Guid? TopicId = null,
    Guid? VocabularyId = null,
    Guid? GrammarId = null) : ICommand<GenerateAndSaveExampleSentencesResponse>;

public sealed record GenerateAndSaveExampleSentencesResponse(
    List<GeneratedExampleSentenceItem> Sentences);

public sealed record GeneratedExampleSentenceItem(
    Guid Id,
    string Text,
    string? Note,
    bool IsAiGenerated,
    Dictionary<string, string> Translations);
