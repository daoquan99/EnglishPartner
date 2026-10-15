using EnglishLiveTutor.Application.Common.Cqrs;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Identity.Contracts;
using EnglishLiveTutor.Domain.Identity.Permissions;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.UpdatePermission;

public sealed record UpdatePermissionCommand(Guid PermissionId, AdminUpdatePermissionRequest Request) : ICommand<PermissionDto?>;

public sealed class UpdatePermissionCommandHandler : ICommandHandler<UpdatePermissionCommand, PermissionDto?>
{
    private readonly IPermissionRepository _permRepo;
    private readonly IUnitOfWork _uow;

    public UpdatePermissionCommandHandler(IPermissionRepository permRepo, IUnitOfWork uow)
    {
        _permRepo = permRepo;
        _uow = uow;
    }

    public async Task<PermissionDto?> HandleAsync(UpdatePermissionCommand command, CancellationToken ct = default)
    {
        var perm = await _permRepo.GetByIdAsync(command.PermissionId, ct);
        if (perm is null)
            return null;

        perm.UpdateDetails(command.Request.Name, command.Request.Module, command.Request.Description);
        await _uow.SaveChangesAsync(ct);
        return new PermissionDto(perm.Id, perm.Code, perm.Name, perm.Module, perm.Description);
    }
}
