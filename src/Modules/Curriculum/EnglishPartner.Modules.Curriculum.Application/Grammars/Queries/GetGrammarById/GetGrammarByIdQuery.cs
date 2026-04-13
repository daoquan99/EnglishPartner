using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Grammars.Queries.GetGrammarById;

public sealed record GetGrammarByIdQuery(Guid Id) : IQuery<GrammarDetailResponse>;

public sealed record GrammarDetailResponse(
    Guid Id,
    string Title,
    string LanguageCode,
    string? Level,
    string? Explanation,
    string? Formula,
    Guid? TopicId,
    List<GrammarExampleDto> Examples);

public sealed record GrammarExampleDto(
    Guid Id,
    string Sentence,
    string? Translation,
    string? Note,
    int SortOrder);
