using EnglishLiveTutor.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.DeletePermission;

public sealed class DeletePermissionCommandHandler : ICommandHandler<DeletePermissionCommand, bool>
{
    private readonly IAppDbContextFactory _dbFactory;
    private readonly ICurrentUser _currentUser;

    public DeletePermissionCommandHandler(IAppDbContextFactory dbFactory, ICurrentUser currentUser)
    {
        _dbFactory = dbFactory;
        _currentUser = currentUser;
    }

    public async Task<bool> HandleAsync(DeletePermissionCommand command, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var perm = await db.Permissions.FirstOrDefaultAsync(p => p.Id == command.PermissionId, ct);
        if (perm is null)
            return false;

        perm.SoftDelete(DateTimeOffset.UtcNow, _currentUser.UserIdOrSystem);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
