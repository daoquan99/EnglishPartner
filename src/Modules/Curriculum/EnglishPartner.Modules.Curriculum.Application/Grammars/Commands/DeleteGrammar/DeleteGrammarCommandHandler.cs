using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Grammars.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Grammars.Commands.DeleteGrammar;

internal sealed class DeleteGrammarCommandHandler(
    IGrammarRepository grammarRepository) : ICommandHandler<DeleteGrammarCommand>
{
    public async Task<Result> Handle(DeleteGrammarCommand request, CancellationToken cancellationToken)
    {
        var grammar = await grammarRepository.GetByIdAsync(request.Id, cancellationToken);
        if (grammar is null) return Result.Failure(CurriculumErrors.Grammar.NotFound(request.Id));

        grammarRepository.Remove(grammar);
        return Result.Success();
    }
}
