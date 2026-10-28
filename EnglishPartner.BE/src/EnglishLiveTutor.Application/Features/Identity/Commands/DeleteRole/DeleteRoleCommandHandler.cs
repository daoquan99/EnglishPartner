using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.DeleteRole;

public sealed class DeleteRoleCommandHandler : ICommandHandler<DeleteRoleCommand, bool>
{
    private readonly IAppDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public DeleteRoleCommandHandler(IAppDbContext db, IUnitOfWork uow, ICurrentUser currentUser)
    {
        _db = db;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<bool> HandleAsync(DeleteRoleCommand command, CancellationToken ct = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == command.RoleId, ct);
        if (role is null)
            return false;

        if (role.IsSystemRole)
            throw new InvalidOperationException($"Không được phép xóa vai trò hệ thống '{role.Code}'.");

        role.SoftDelete(DateTimeOffset.UtcNow, _currentUser.UserIdOrSystem);
        await _uow.CommitChangesAsync(ct);
        return true;
    }
}
