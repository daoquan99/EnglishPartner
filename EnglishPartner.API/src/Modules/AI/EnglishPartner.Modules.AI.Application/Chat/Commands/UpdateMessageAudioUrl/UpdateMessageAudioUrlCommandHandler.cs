using EnglishPartner.Modules.AI.Domain;
using EnglishPartner.Modules.AI.Domain.ChatSessions.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.AI.Application.Chat.Commands.UpdateMessageAudioUrl;

internal sealed class UpdateMessageAudioUrlCommandHandler(
    IChatSessionRepository repository,
    ICurrentUserService currentUser) : ICommandHandler<UpdateMessageAudioUrlCommand>
{
    public async Task<Result> Handle(UpdateMessageAudioUrlCommand request, CancellationToken cancellationToken)
    {
        var session = await repository.GetByIdWithMessagesAsync(request.SessionId, cancellationToken);
        
        if (session is null)
            return Result.Failure(AiErrors.ChatSession.NotFound);

        if (session.UserId != currentUser.UserId!.Value)
            return Result.Failure(AiErrors.ChatSession.AccessDenied);

        session.UpdateMessageAudioUrl(request.MessageId, request.AudioUrl);

        return Result.Success();
    }
}
