using EnglishPartner.Modules.Curriculum.Domain.Languages;
using EnglishPartner.Modules.Curriculum.Domain.Languages.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Languages.Commands.CreateLanguage;

internal sealed class CreateLanguageCommandHandler(ILanguageRepository languageRepository) : ICommandHandler<CreateLanguageCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateLanguageCommand request, CancellationToken cancellationToken)
    {
        var existing = await languageRepository.GetByCodeAsync(request.Code, cancellationToken);
        if (existing is not null)
        {
            return Result.Failure<Guid>(new Error("Curriculum.Language.AlreadyExists", "A language with this code already exists."));
        }

        var language = Language.Create(request.Code, request.Name, request.NativeName, request.SortOrder);
        languageRepository.Add(language);

        return language.Id;
    }
}
