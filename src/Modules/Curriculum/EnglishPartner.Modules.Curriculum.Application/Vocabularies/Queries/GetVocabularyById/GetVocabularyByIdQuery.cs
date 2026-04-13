using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Vocabularies.Queries.GetVocabularyById;

public sealed record GetVocabularyByIdQuery(Guid Id) : IQuery<VocabularyDetailResponse>;

public sealed record VocabularyDetailResponse(
    Guid Id,
    string Word,
    string Definition,
    string LanguageCode,
    string? Pronunciation,
    string? PhoneticTranscription,
    string? PartOfSpeech,
    string? ExampleSentence,
    string? ExampleTranslation,
    string? ImageUrl,
    string? AudioUrl,
    string? Level,
    Guid? TopicId,
    List<string> Synonyms);
