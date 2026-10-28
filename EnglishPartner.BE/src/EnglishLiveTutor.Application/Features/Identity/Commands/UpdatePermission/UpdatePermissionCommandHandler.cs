using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.UpdatePermission;

public sealed class UpdatePermissionCommandHandler : ICommandHandler<UpdatePermissionCommand, PermissionDto?>
{
    private readonly IAppDbContext _db;
    private readonly IUnitOfWork _uow;

    public UpdatePermissionCommandHandler(IAppDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<PermissionDto?> HandleAsync(UpdatePermissionCommand command, CancellationToken ct = default)
    {
        var perm = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == command.PermissionId, ct);
        if (perm is null)
            return null;

        perm.UpdateDetails(command.Request.Name, command.Request.Module, command.Request.Description);
        await _uow.CommitChangesAsync(ct);
        return new PermissionDto(perm.Id, perm.Code, perm.Name, perm.Module, perm.Description);
    }
}
