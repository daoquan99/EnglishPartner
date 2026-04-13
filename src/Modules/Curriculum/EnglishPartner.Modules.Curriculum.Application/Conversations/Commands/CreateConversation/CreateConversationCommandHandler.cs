using EnglishPartner.Modules.Curriculum.Domain.Conversations;
using EnglishPartner.Modules.Curriculum.Domain.Conversations.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.CreateConversation;

internal sealed class CreateConversationCommandHandler(
    IConversationRepository conversationRepository) : ICommandHandler<CreateConversationCommand, Guid>
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
        return conversation.Id;
    }
}
