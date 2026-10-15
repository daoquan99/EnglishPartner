using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Commands.DeleteScoringRubric;

public sealed record DeleteScoringRubricCommand(string Code) : ICommand<bool>;

public sealed class DeleteScoringRubricCommandHandler(IScoringRubricRepository repository, ICurrentUser currentUser, TimeProvider timeProvider)
    : ICommandHandler<DeleteScoringRubricCommand, bool>
{
    public async Task<bool> HandleAsync(DeleteScoringRubricCommand command, CancellationToken ct = default)
    {
        var entity = await repository.GetByCodeWithCriteriaAsync(command.Code, ct);
        if (entity is null) return false;
        entity.SoftDelete(timeProvider.GetUtcNow(), currentUser.UserIdOrSystem);
        await repository.SaveChangesAsync(ct);
        return true;
    }
}
