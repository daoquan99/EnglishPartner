using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.ExampleSentences.Commands.UpdateExampleSentence;

public sealed record UpdateExampleSentenceCommand(
    Guid Id,
    string Text,
    string? Translation,
    string? AudioUrl,
    string? Note,
    string? Level,
    Guid? TopicId,
    Guid? VocabularyId,
    Guid? GrammarId) : ICommand;
