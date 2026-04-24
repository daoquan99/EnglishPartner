using EnglishPartner.Modules.Curriculum.Domain.Languages.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Languages.Commands.DeleteLanguage;

internal sealed class DeleteLanguageCommandHandler(ILanguageRepository languageRepository) : ICommandHandler<DeleteLanguageCommand>
{
    public async Task<Result> Handle(DeleteLanguageCommand request, CancellationToken cancellationToken)
    {
        var language = await languageRepository.GetByIdAsync(request.Id, cancellationToken);
        if (language is null)
        {
            return Result.Failure(new Error("Curriculum.Language.NotFound", "The language was not found."));
        }

        languageRepository.Remove(language);

        return Result.Success();
    }
}
