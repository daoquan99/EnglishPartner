namespace EnglishPartner.Modules.Curriculum.Application.Languages.Queries.GetLanguages;

public sealed record LanguageResponse(
    Guid Id,
    string Code,
    string Name,
    string NativeName,
    int SortOrder);
