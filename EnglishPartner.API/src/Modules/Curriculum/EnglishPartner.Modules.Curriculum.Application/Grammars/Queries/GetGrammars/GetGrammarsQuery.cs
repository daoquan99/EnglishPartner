using EnglishPartner.SharedKernel.Application.Caching;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Grammars.Queries.GetGrammars;

public sealed record GetGrammarsQuery(
    string LanguageCode,
    int Page = 1,
    int PageSize = 20,
    string? SearchTerm = null) : ICacheableQuery<GrammarsResponse>
{
    public string CacheKey   => $"curriculum:grammars:{LanguageCode}:p{Page}:s{PageSize}:q{SearchTerm ?? "all"}";
    public TimeSpan? Expiration => TimeSpan.FromHours(6);
}

public sealed record GrammarsResponse(
    List<GrammarListItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record GrammarListItemResponse(
    Guid Id,
    string Title,
    string? Formula,
    string? Level,
    Guid? TopicId);
