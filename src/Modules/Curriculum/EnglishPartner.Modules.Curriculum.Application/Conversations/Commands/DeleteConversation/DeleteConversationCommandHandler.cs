using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Conversations.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;
using EnglishPartner.SharedKernel.Application.Caching;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.DeleteConversation;

internal sealed class DeleteConversationCommandHandler(
    IConversationRepository conversationRepository,
    ICurrentUserService currentUser,
    TimeProvider timeProvider,
    ICacheService cacheService) : ICommandHandler<DeleteConversationCommand>
{
    public async Task<Result> Handle(DeleteConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = await conversationRepository.GetByIdAsync(request.Id, cancellationToken);
        if (conversation is null) return Result.Failure(CurriculumErrors.Conversation.NotFound(request.Id));

        conversationRepository.Remove(conversation, currentUser.UserId, timeProvider.GetUtcNow().UtcDateTime);

        await cacheService.RemoveByPrefixAsync(CacheKeys.Curriculum.ConversationsPrefix(conversation.LanguageCode), cancellationToken);

        return Result.Success();
    }
}
