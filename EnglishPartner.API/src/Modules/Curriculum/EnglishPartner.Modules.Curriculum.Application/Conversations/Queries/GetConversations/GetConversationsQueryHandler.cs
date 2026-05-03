using EnglishPartner.Modules.Curriculum.Domain.Conversations.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Queries.GetConversations;

internal sealed class GetConversationsQueryHandler(
    IConversationRepository conversationRepository) : IQueryHandler<GetConversationsQuery, ConversationsResponse>
{
    public async Task<Result<ConversationsResponse>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await conversationRepository.GetPagedAsync(
            request.LanguageCode, request.Page, request.PageSize, request.TopicId, request.Level, cancellationToken);
        
        var dtos = items.Select(c => new ConversationListItemResponse(
            c.Id, c.Title, c.Scenario, c.Level, c.ImageUrl)).ToList();

        return new ConversationsResponse(dtos, totalCount, request.Page, request.PageSize);
    }
}
