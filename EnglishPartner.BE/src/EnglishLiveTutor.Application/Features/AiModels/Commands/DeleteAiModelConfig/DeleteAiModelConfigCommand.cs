using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.AiCatalog.Repositories;

namespace EnglishLiveTutor.Application.Features.AiModels.Commands.DeleteAiModelConfig;

public sealed record DeleteAiModelConfigCommand(Guid Id) : ICommand<bool>;

public sealed class DeleteAiModelConfigCommandHandler(IAiModelCatalogRepository repository, ICurrentUser currentUser, TimeProvider timeProvider)
    : ICommandHandler<DeleteAiModelConfigCommand, bool>
{
    public async Task<bool> HandleAsync(DeleteAiModelConfigCommand command, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(command.Id, ct);
        if (entity is null) return false;
        entity.SoftDelete(timeProvider.GetUtcNow(), currentUser.UserIdOrSystem);
        await repository.SaveChangesAsync(ct);
        return true;
    }
}
