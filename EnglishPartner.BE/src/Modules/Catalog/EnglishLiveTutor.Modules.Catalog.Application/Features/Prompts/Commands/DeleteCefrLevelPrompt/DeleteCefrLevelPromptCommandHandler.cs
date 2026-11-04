using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Prompts.Repositories;

namespace EnglishLiveTutor.Application.Features.Prompts.Commands.DeleteCefrLevelPrompt;

public sealed class DeleteCefrLevelPromptCommandHandler(IPromptConfigurationRepository repository, ICurrentUser currentUser, TimeProvider timeProvider)
    : ICommandHandler<DeleteCefrLevelPromptCommand, bool>
{
    public async Task<bool> HandleAsync(DeleteCefrLevelPromptCommand command, CancellationToken ct = default)
    {
        var entity = await repository.GetCefrLevelPromptAsync(command.CefrLevel, ct);
        if (entity is null) return false;
        entity.SoftDelete(timeProvider.GetUtcNow(), currentUser.UserIdOrSystem);
        await repository.SaveChangesAsync(ct);
        return true;
    }
}
