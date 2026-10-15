using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Prompts.Repositories;

namespace EnglishLiveTutor.Application.Features.Prompts.Commands.DeleteConversationModePrompt;

public sealed record DeleteConversationModePromptCommand(string ModeCode) : ICommand<bool>;

public sealed class DeleteConversationModePromptCommandHandler(IPromptConfigurationRepository repository, ICurrentUser currentUser, TimeProvider timeProvider)
    : ICommandHandler<DeleteConversationModePromptCommand, bool>
{
    public async Task<bool> HandleAsync(DeleteConversationModePromptCommand command, CancellationToken ct = default)
    {
        var entity = await repository.GetConversationModePromptAsync(command.ModeCode, ct);
        if (entity is null) return false;
        entity.SoftDelete(timeProvider.GetUtcNow(), currentUser.UserIdOrSystem);
        await repository.SaveChangesAsync(ct);
        return true;
    }
}
