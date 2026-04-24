using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.ExampleSentences.Commands.CreateExampleSentence;

public sealed record CreateExampleSentenceCommand(
    string Text,
    string LanguageCode,
    Dictionary<string, string>? Translations,
    string? AudioUrl,
    string? Note,
    string? Level,
    Guid? TopicId,
    Guid? VocabularyId,
    Guid? GrammarId) : ICommand<Guid>;
