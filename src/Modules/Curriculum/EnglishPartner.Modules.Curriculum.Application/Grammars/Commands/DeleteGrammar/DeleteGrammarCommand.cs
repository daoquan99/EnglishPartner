using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Grammars.Commands.DeleteGrammar;

public sealed record DeleteGrammarCommand(Guid Id) : ICommand;
