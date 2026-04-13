using EnglishPartner.Modules.Curriculum.Domain.Conversations.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.AddConversationLine;

internal sealed class AddConversationLineCommandHandler(
    IConversationRepository conversationRepository) : ICommandHandler<AddConversationLineCommand>
{
    public async Task<Result> Handle(AddConversationLineCommand request, CancellationToken cancellationToken)
    {
        var conversation = await conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null) return Result.Failure(new Error("Conversation.NotFound", "Conversation not found."));

        conversation.AddLine(request.Speaker, request.Text, request.Translation, request.AudioUrl);
        
        conversationRepository.Update(conversation);
        return Result.Success();
    }
}
