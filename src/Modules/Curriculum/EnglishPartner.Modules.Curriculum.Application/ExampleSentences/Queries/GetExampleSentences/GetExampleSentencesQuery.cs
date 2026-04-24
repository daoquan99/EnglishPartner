using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.ExampleSentences.Queries.GetExampleSentences;

public sealed record GetExampleSentencesQuery(
    string LanguageCode = "en",
    int Page = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    Guid? VocabularyId = null,
    Guid? GrammarId = null,
    Guid? TopicId = null) : IQuery<ExampleSentencesResponse>;

public sealed record ExampleSentencesResponse(
    List<ExampleSentenceResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ExampleSentenceResponse(
    Guid Id,
    string Text,
    string? AudioUrl,
    string? Note,
    string? Level,
    List<ExampleSentenceTranslationDto> Translations);

public sealed record ExampleSentenceTranslationDto(string LanguageCode, string TranslatedText);
