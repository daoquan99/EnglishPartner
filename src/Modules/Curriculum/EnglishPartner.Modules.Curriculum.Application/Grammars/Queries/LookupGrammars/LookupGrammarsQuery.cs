using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Grammars.Queries.LookupGrammars;

public sealed record LookupGrammarsQuery(
    string LanguageCode,
    string? SearchTerm,
    int Page,
    int PageSize) : IQuery<LookupGrammarsResponse>;

public sealed record LookupGrammarsResponse(
    List<GrammarLookupItem> Items,
    int TotalCount);

public sealed record GrammarLookupItem(
    Guid Id,
    string Label);
