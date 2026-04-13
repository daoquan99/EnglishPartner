using EnglishPartner.Modules.Curriculum.Domain.Conversations.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Queries.GetConversations;

internal sealed class GetConversationsQueryHandler(
    IConversationRepository conversationRepository) : IQueryHandler<GetConversationsQuery, List<ConversationListItemResponse>>
{
    public async Task<Result<List<ConversationListItemResponse>>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var conversations = await conversationRepository.GetByLanguageCodeAsync(request.LanguageCode, cancellationToken);
        
        return conversations.Select(c => new ConversationListItemResponse(
            c.Id, c.Title, c.Scenario, c.Level, c.ImageUrl)).ToList();
    }
}
