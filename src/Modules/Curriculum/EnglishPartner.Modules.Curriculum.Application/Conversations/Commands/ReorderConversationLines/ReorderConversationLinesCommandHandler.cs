using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Conversations.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.ReorderConversationLines;

internal sealed class ReorderConversationLinesCommandHandler(
    IConversationRepository conversationRepository) : ICommandHandler<ReorderConversationLinesCommand>
{
    public async Task<Result> Handle(ReorderConversationLinesCommand request, CancellationToken cancellationToken)
    {
        var conversation = await conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null) return Result.Failure(CurriculumErrors.Conversation.NotFound(request.ConversationId));

        conversation.ReorderLines(request.OrderedLineIds);
        
        conversationRepository.Update(conversation);
        return Result.Success();
    }
}
