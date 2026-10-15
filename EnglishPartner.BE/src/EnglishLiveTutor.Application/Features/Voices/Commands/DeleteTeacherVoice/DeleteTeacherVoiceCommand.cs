using EnglishLiveTutor.Application.Common.Cqrs;
using EnglishLiveTutor.Application.Common.Identity;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Domain.Voices;

namespace EnglishLiveTutor.Application.Features.Voices.Commands.DeleteTeacherVoice;

public sealed record DeleteTeacherVoiceCommand(string Id) : ICommand<bool>;

public sealed class DeleteTeacherVoiceCommandHandler(
    ITeacherVoiceRepository voiceRepo,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<DeleteTeacherVoiceCommand, bool>
{
    public async Task<bool> HandleAsync(DeleteTeacherVoiceCommand command, CancellationToken ct = default)
    {
        var voice = await voiceRepo.GetByIdAsync(command.Id, ct);
        if (voice is null) return false;

        voice.SoftDelete(timeProvider.GetUtcNow(), currentUser.UserIdOrSystem);
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}
