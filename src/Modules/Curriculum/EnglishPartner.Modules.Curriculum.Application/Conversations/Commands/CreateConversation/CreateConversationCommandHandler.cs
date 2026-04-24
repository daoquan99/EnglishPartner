using EnglishPartner.Modules.Curriculum.Domain.Conversations;
using EnglishPartner.Modules.Curriculum.Domain.Conversations.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;
using EnglishPartner.SharedKernel.Application.Caching;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.CreateConversation;

internal sealed class CreateConversationCommandHandler(
    IConversationRepository conversationRepository,
    ICacheService cacheService) : ICommandHandler<CreateConversationCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = Conversation.Create(
            request.Title,
            request.LanguageCode,
            request.Scenario,
            request.Level,
            request.ImageUrl,
            request.TopicId,
            request.GrammarId);

        conversationRepository.Add(conversation);
        
        await cacheService.RemoveByPrefixAsync(CacheKeys.Curriculum.ConversationsPrefix(request.LanguageCode), cancellationToken);

        return conversation.Id;
    }
}
