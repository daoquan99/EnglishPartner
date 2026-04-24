using EnglishPartner.Modules.Curriculum.Domain.Languages.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Languages.Commands.UpdateLanguage;

internal sealed class UpdateLanguageCommandHandler(ILanguageRepository languageRepository) : ICommandHandler<UpdateLanguageCommand>
{
    public async Task<Result> Handle(UpdateLanguageCommand request, CancellationToken cancellationToken)
    {
        var language = await languageRepository.GetByIdAsync(request.Id, cancellationToken);
        if (language is null)
        {
            return Result.Failure(new Error("Curriculum.Language.NotFound", "The language was not found."));
        }

        language.Update(request.Name, request.NativeName, request.SortOrder);
        languageRepository.Update(language);

        return Result.Success();
    }
}
