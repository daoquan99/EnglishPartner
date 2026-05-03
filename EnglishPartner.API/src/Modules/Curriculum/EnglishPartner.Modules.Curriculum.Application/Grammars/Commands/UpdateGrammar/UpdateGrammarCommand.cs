using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Grammars.Commands.UpdateGrammar;

public sealed record UpdateGrammarCommand(
    Guid Id,
    string Title,
    string? Level,
    string? Explanation,
    string? Formula,
    Guid? TopicId) : ICommand;
