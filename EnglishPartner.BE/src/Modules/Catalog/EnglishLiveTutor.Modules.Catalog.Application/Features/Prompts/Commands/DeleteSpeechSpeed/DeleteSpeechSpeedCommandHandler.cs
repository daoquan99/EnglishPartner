using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Prompts.Repositories;

namespace EnglishLiveTutor.Application.Features.Prompts.Commands.DeleteSpeechSpeed;

public sealed class DeleteSpeechSpeedCommandHandler(ISpeechSpeedRepository repository, ICurrentUser currentUser, TimeProvider timeProvider)
    : ICommandHandler<DeleteSpeechSpeedCommand, bool>
{
    public async Task<bool> HandleAsync(DeleteSpeechSpeedCommand command, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(command.Id, ct);
        if (entity is null) return false;
        entity.SoftDelete(timeProvider.GetUtcNow(), currentUser.UserIdOrSystem);
        await repository.SaveChangesAsync(ct);
        return true;
    }
}
