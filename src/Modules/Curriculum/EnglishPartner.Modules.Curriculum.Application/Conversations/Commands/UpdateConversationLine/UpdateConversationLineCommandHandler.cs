using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Conversations.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.UpdateConversationLine;

internal sealed class UpdateConversationLineCommandHandler(
    IConversationRepository repository) : ICommandHandler<UpdateConversationLineCommand>
{
    public async Task<Result> Handle(UpdateConversationLineCommand request, CancellationToken cancellationToken)
    {
        var conversation = await repository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null) return Result.Failure(CurriculumErrors.Conversation.NotFound(request.ConversationId));

        var line = conversation.UpdateLine(request.LineId, request.Speaker, request.Text, request.AudioUrl);
        if (line is null) return Result.Failure(CurriculumErrors.Conversation.LineNotFound(request.LineId));

        if (request.Translations is { Count: > 0 })
        {
            foreach (var (langCode, translatedText) in request.Translations)
            {
                line.AddTranslation(langCode, translatedText);
            }
        }

        repository.Update(conversation);
        return Result.Success();
    }
}
