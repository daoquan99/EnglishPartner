using EnglishPartner.Modules.Curriculum.Domain.Conversations.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.UpdateConversation;

internal sealed class UpdateConversationCommandHandler(
    IConversationRepository conversationRepository) : ICommandHandler<UpdateConversationCommand>
{
    public async Task<Result> Handle(UpdateConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = await conversationRepository.GetByIdAsync(request.Id, cancellationToken);
        if (conversation is null) return Result.Failure(new Error("Conversation.NotFound", "Conversation not found."));

        conversation.Update(
            request.Title,
            request.Scenario,
            request.Level,
            request.ImageUrl,
            request.TopicId,
            request.GrammarId);

        conversationRepository.Update(conversation);
        return Result.Success();
    }
}
