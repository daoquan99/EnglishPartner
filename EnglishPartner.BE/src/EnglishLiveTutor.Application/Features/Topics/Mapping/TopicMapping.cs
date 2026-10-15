using EnglishLiveTutor.Application.Features.Topics.Contracts;
using EnglishLiveTutor.Domain.Topics;

namespace EnglishLiveTutor.Application.Features.Topics.Mapping;

public static class TopicMapping
{
    public static TopicDto ToDto(this TopicEntity t) => new(
        t.Id,
        t.Code,
        t.Name,
        t.DisplayNameVi,
        t.Category,
        t.ModeCode,
        t.SuggestedCefrLevel,
        t.SupportsConversation,
        t.SupportsVocabulary,
        t.Description,
        t.IsFeatured,
        t.IsActive,
        t.DisplayOrder
    );
}
