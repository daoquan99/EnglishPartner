using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateAndSaveVocabulary;

/// <summary>
/// Admin command: Ask AI to generate vocabulary entries and save them.
/// When Count=1, WordHint is the specific word. When Count>1, it is treated as a topic context.
/// If AutoGenerateExamples=true, example sentences are also generated and saved per vocabulary.
/// </summary>
public sealed record GenerateAndSaveVocabularyCommand(
    string TargetLanguage,
    string WordHint,
    int Count = 1,
    string? Level = null,
    Guid? TopicId = null,
    bool AutoGenerateExamples = false,
    int ExampleCount = 3,
    List<string>? ExampleTargetLanguageCodes = null) : ICommand<GenerateAndSaveVocabularyResponse>;

public sealed record GenerateAndSaveVocabularyResponse(
    List<GeneratedVocabularyItem> Items);

public sealed record GeneratedVocabularyItem(
    Guid Id,
    string Word,
    string Definition,
    string? Pronunciation,
    string? PhoneticTranscription,
    string? PartOfSpeech,
    string? Level,
    List<string> Synonyms,
    bool IsAiGenerated,
    List<Guid> GeneratedExampleSentenceIds);
