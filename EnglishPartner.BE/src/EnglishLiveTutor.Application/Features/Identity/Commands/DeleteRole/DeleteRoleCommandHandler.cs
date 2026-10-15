using EnglishLiveTutor.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.DeleteRole;

public sealed class DeleteRoleCommandHandler : ICommandHandler<DeleteRoleCommand, bool>
{
    private readonly IAppDbContextFactory _dbFactory;
    private readonly ICurrentUser _currentUser;

    public DeleteRoleCommandHandler(IAppDbContextFactory dbFactory, ICurrentUser currentUser)
    {
        _dbFactory = dbFactory;
        _currentUser = currentUser;
    }

    public async Task<bool> HandleAsync(DeleteRoleCommand command, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == command.RoleId, ct);
        if (role is null)
            return false;

        if (role.IsSystemRole)
            throw new InvalidOperationException($"Không được phép xóa vai trò hệ thống '{role.Code}'.");

        role.SoftDelete(DateTimeOffset.UtcNow, _currentUser.UserIdOrSystem);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
