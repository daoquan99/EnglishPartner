using EnglishLiveTutor.Application.Common.Cqrs;
using EnglishLiveTutor.Application.Common.Identity;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Domain.Identity.Roles;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.DeleteRole;

public sealed record DeleteRoleCommand(Guid RoleId) : ICommand<bool>;

public sealed class DeleteRoleCommandHandler : ICommandHandler<DeleteRoleCommand, bool>
{
    private readonly IRoleRepository _roleRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public DeleteRoleCommandHandler(
        IRoleRepository roleRepo,
        IUnitOfWork uow,
        ICurrentUser currentUser,
        TimeProvider? timeProvider = null)
    {
        _roleRepo = roleRepo;
        _uow = uow;
        _currentUser = currentUser;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<bool> HandleAsync(DeleteRoleCommand command, CancellationToken ct = default)
    {
        var role = await _roleRepo.GetByIdWithPermissionsAsync(command.RoleId, ct);
        if (role is null)
            return false;

        role.SoftDelete(_timeProvider.GetUtcNow(), _currentUser.UserIdOrSystem);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
