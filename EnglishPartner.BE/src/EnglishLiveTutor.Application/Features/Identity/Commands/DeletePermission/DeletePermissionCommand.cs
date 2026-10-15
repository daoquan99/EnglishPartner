using EnglishLiveTutor.Application.Common.Cqrs;
using EnglishLiveTutor.Application.Common.Identity;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Domain.Identity.Permissions;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.DeletePermission;

public sealed record DeletePermissionCommand(Guid PermissionId) : ICommand<bool>;

public sealed class DeletePermissionCommandHandler : ICommandHandler<DeletePermissionCommand, bool>
{
    private readonly IPermissionRepository _permRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public DeletePermissionCommandHandler(
        IPermissionRepository permRepo,
        IUnitOfWork uow,
        ICurrentUser currentUser,
        TimeProvider? timeProvider = null)
    {
        _permRepo = permRepo;
        _uow = uow;
        _currentUser = currentUser;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<bool> HandleAsync(DeletePermissionCommand command, CancellationToken ct = default)
    {
        var perm = await _permRepo.GetByIdAsync(command.PermissionId, ct);
        if (perm is null)
            return false;

        perm.SoftDelete(_timeProvider.GetUtcNow(), _currentUser.UserIdOrSystem);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
