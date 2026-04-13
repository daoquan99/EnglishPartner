using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Queries.GetConversationById;

public sealed record GetConversationByIdQuery(Guid Id) : IQuery<ConversationDetailResponse>;

public sealed record ConversationDetailResponse(
    Guid Id,
    string Title,
    string LanguageCode,
    string? Scenario,
    string? Level,
    string? ImageUrl,
    Guid? TopicId,
    Guid? GrammarId,
    List<ConversationLineDto> Lines);

public sealed record ConversationLineDto(
    Guid Id,
    string Speaker,
    string Text,
    string? Translation,
    string? AudioUrl,
    int SortOrder);
