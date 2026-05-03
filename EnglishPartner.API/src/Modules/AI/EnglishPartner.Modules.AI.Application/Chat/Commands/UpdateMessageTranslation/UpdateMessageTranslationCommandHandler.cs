using EnglishPartner.Modules.AI.Domain;
using EnglishPartner.Modules.AI.Domain.ChatSessions.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.AI.Application.Chat.Commands.UpdateMessageTranslation;

internal sealed class UpdateMessageTranslationCommandHandler(
    IChatSessionRepository repository,
    ICurrentUserService currentUser) : ICommandHandler<UpdateMessageTranslationCommand>
{
    public async Task<Result> Handle(UpdateMessageTranslationCommand request, CancellationToken cancellationToken)
    {
        var session = await repository.GetByIdWithMessagesAsync(request.SessionId, cancellationToken);
        
        if (session is null)
            return Result.Failure(AiErrors.ChatSession.NotFound);

        if (session.UserId != currentUser.UserId!.Value)
            return Result.Failure(AiErrors.ChatSession.AccessDenied);

        session.UpdateMessageTranslation(request.MessageId, request.Translation);

        return Result.Success();
    }
}
