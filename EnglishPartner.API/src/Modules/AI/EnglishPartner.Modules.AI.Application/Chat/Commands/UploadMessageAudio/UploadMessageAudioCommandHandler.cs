using EnglishPartner.Modules.AI.Domain;
using EnglishPartner.Modules.AI.Domain.ChatSessions.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.AI.Application.Chat.Commands.UploadMessageAudio;

internal sealed class UploadMessageAudioCommandHandler(
    IChatSessionRepository repository,
    ICurrentUserService currentUser,
    IFileStorageService fileStorageService) : ICommandHandler<UploadMessageAudioCommand, UploadMessageAudioResponse>
{
    public async Task<Result<UploadMessageAudioResponse>> Handle(UploadMessageAudioCommand request, CancellationToken cancellationToken)
    {
        var session = await repository.GetByIdWithMessagesAsync(request.SessionId, cancellationToken);
        
        if (session is null)
            return Result.Failure<UploadMessageAudioResponse>(AiErrors.ChatSession.NotFound);

        if (session.UserId != currentUser.UserId!.Value)
            return Result.Failure<UploadMessageAudioResponse>(AiErrors.ChatSession.AccessDenied);

        var message = session.Messages.FirstOrDefault(m => m.Id == request.MessageId);
        if (message is null)
            return Result.Failure<UploadMessageAudioResponse>(AiErrors.ChatSession.NotFound);

        var extension = Path.GetExtension(request.FileName);
        var randomName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = await fileStorageService.UploadAsync(request.AudioStream, randomName, "chat_audio", cancellationToken);
        
        var audioUrl = fileStorageService.GetPublicUrl(relativePath);
        session.UpdateMessageAudioUrl(request.MessageId, audioUrl);

        return new UploadMessageAudioResponse(audioUrl);
    }
}
