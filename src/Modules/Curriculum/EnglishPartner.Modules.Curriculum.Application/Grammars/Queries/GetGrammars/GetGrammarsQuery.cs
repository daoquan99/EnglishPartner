using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Grammars.Queries.GetGrammars;

public sealed record GetGrammarsQuery(string LanguageCode) : IQuery<List<GrammarListItemResponse>>;

public sealed record GrammarListItemResponse(
    Guid Id,
    string Title,
    string? Level,
    Guid? TopicId);
