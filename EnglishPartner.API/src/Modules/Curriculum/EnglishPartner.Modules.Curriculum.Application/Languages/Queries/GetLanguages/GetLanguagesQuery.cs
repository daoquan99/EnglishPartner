using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Languages.Queries.GetLanguages;

public sealed record GetLanguagesQuery : IQuery<IReadOnlyList<LanguageResponse>>;
