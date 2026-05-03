using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.AI.Application.Chat.Commands.UploadMessageAudio;

public sealed record UploadMessageAudioCommand(
    Guid SessionId,
    Guid MessageId,
    Stream AudioStream,
    string FileName,
    string ContentType) : ICommand<UploadMessageAudioResponse>;

public sealed record UploadMessageAudioResponse(
    string AudioUrl);
