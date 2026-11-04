using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Modules.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.DeletePermission;

public sealed class DeletePermissionCommandHandler : ICommandHandler<DeletePermissionCommand, bool>
{
    private readonly IIdentityDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public DeletePermissionCommandHandler(IIdentityDbContext db, IUnitOfWork uow, ICurrentUser currentUser)
    {
        _db = db;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<bool> HandleAsync(DeletePermissionCommand command, CancellationToken ct = default)
    {
        var perm = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == command.PermissionId, ct);
        if (perm is null)
            return false;

        perm.SoftDelete(DateTimeOffset.UtcNow, _currentUser.UserIdOrSystem);
        await _uow.CommitChangesAsync(ct);
        return true;
    }
}
