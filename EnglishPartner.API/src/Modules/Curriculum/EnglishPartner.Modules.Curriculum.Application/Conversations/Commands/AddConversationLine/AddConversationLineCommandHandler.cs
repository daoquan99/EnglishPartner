using EnglishPartner.Modules.Curriculum.Domain;
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
        if (conversation is null) return Result.Failure(CurriculumErrors.Conversation.NotFound(request.ConversationId));

        var line = conversation.AddLine(request.Speaker, request.Text, request.AudioUrl);
        if (request.Translations is { Count: > 0 })
        {
            foreach (var (langCode, text) in request.Translations)
                line.AddTranslation(langCode, text);
        }
        conversationRepository.Update(conversation);
        return Result.Success();
    }
}
