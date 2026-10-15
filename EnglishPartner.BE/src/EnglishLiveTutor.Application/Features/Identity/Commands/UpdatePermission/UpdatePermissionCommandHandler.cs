using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.UpdatePermission;

public sealed class UpdatePermissionCommandHandler : ICommandHandler<UpdatePermissionCommand, PermissionDto?>
{
    private readonly IAppDbContextFactory _dbFactory;

    public UpdatePermissionCommandHandler(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<PermissionDto?> HandleAsync(UpdatePermissionCommand command, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var perm = await db.Permissions.FirstOrDefaultAsync(p => p.Id == command.PermissionId, ct);
        if (perm is null)
            return null;

        perm.UpdateDetails(command.Request.Name, command.Request.Module, command.Request.Description);
        await db.SaveChangesAsync(ct);
        return new PermissionDto(perm.Id, perm.Code, perm.Name, perm.Module, perm.Description);
    }
}
