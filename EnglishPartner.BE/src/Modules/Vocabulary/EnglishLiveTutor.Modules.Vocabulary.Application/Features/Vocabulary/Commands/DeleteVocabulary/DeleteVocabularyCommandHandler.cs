using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Vocabulary.Repositories;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.DeleteVocabulary;

public sealed class DeleteVocabularyCommandHandler(IVocabularyRepository repository, ICurrentUser currentUser, TimeProvider timeProvider)
    : ICommandHandler<DeleteVocabularyCommand, bool>
{
    public async Task<bool> HandleAsync(DeleteVocabularyCommand command, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdWithDetailsAsync(command.Id, ct);
        if (entity is null) return false;
        entity.SoftDelete(timeProvider.GetUtcNow(), currentUser.UserIdOrSystem);
        await repository.SaveChangesAsync(ct);
        return true;
    }
}
