using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.ExampleSentences.Queries.GetExampleSentences;

// Can be queried by VocabularyId, GrammarId, or TopicId
public sealed record GetExampleSentencesQuery(
    Guid? VocabularyId = null,
    Guid? GrammarId = null,
    Guid? TopicId = null) : IQuery<List<ExampleSentenceResponse>>;

public sealed record ExampleSentenceResponse(
    Guid Id,
    string Text,
    string? Translation,
    string? AudioUrl,
    string? Note,
    string? Level);
