using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Grammars.Commands.CreateGrammar;

public sealed record CreateGrammarCommand(
    string Title,
    string LanguageCode,
    string? Level,
    string? Explanation,
    string? Formula,
    Guid? TopicId) : ICommand<Guid>;
