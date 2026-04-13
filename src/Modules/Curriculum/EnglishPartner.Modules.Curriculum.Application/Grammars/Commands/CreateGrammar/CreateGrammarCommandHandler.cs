using EnglishPartner.Modules.Curriculum.Domain.Grammars;
using EnglishPartner.Modules.Curriculum.Domain.Grammars.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Grammars.Commands.CreateGrammar;

internal sealed class CreateGrammarCommandHandler(
    IGrammarRepository grammarRepository) : ICommandHandler<CreateGrammarCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateGrammarCommand request, CancellationToken cancellationToken)
    {
        var grammar = Grammar.Create(
            request.Title,
            request.LanguageCode,
            request.Level,
            request.Explanation,
            request.Formula,
            request.TopicId);

        grammarRepository.Add(grammar);
        return grammar.Id;
    }
}
