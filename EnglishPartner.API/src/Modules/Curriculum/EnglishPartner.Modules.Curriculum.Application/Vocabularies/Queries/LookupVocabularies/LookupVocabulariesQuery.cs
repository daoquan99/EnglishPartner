using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Vocabularies.Queries.LookupVocabularies;

public sealed record LookupVocabulariesQuery(
    string LanguageCode,
    string? SearchTerm,
    int Page,
    int PageSize) : IQuery<LookupVocabulariesResponse>;

public sealed record LookupVocabulariesResponse(
    List<VocabularyLookupItem> Items,
    int TotalCount);

public sealed record VocabularyLookupItem(
    Guid Id,
    string Label);
