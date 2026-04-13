using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Vocabularies.Queries.GetVocabularies;

public sealed record GetVocabulariesQuery(
    string LanguageCode = "en",
    int Page = 1,
    int PageSize = 20) : IQuery<VocabulariesResponse>;

public sealed record VocabulariesResponse(
    List<VocabularyListItem> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record VocabularyListItem(
    Guid Id,
    string Word,
    string Definition,
    string? PartOfSpeech,
    string? Level);
