using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Grammars.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Grammars.Commands.UpdateGrammar;

internal sealed class UpdateGrammarCommandHandler(
    IGrammarRepository grammarRepository) : ICommandHandler<UpdateGrammarCommand>
{
    public async Task<Result> Handle(UpdateGrammarCommand request, CancellationToken cancellationToken)
    {
        var grammar = await grammarRepository.GetByIdAsync(request.Id, cancellationToken);
        if (grammar is null) return Result.Failure(CurriculumErrors.Grammar.NotFound(request.Id));

        grammar.Update(
            request.Title,
            request.Level,
            request.Explanation,
            request.Formula,
            request.TopicId);

        grammarRepository.Update(grammar);
        return Result.Success();
    }
}
