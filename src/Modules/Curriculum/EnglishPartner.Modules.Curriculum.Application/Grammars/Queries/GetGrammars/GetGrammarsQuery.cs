using EnglishPartner.SharedKernel.Application.Caching;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Grammars.Queries.GetGrammars;

public sealed record GetGrammarsQuery(string LanguageCode) : ICacheableQuery<List<GrammarListItemResponse>>
{
    public string CacheKey   => $"Grammars:{LanguageCode}";
    public TimeSpan? Expiration => TimeSpan.FromHours(12);
}

public sealed record GrammarListItemResponse(
    Guid Id,
    string Title,
    string? Level,
    Guid? TopicId);
