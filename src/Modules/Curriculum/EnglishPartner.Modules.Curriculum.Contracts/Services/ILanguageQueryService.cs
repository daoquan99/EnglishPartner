namespace EnglishPartner.Modules.Curriculum.Contracts.Services;

public interface ILanguageQueryService
{
    Task<List<LanguageDto>> GetSupportedLanguagesAsync(CancellationToken cancellationToken = default);
}

public sealed record LanguageDto(string Code, string Name, string NativeName, int SortOrder);
