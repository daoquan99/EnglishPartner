using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Identity.Repositories;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.CreatePermission;

public sealed class CreatePermissionCommandHandler : ICommandHandler<CreatePermissionCommand, PermissionDto>
{
    private readonly IPermissionRepository _permRepo;
    private readonly IUnitOfWork _uow;

    public CreatePermissionCommandHandler(IPermissionRepository permRepo, IUnitOfWork uow)
    {
        _permRepo = permRepo;
        _uow = uow;
    }

    public async Task<PermissionDto> HandleAsync(CreatePermissionCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var existing = await _permRepo.GetByCodeAsync(req.Code, ct);
        if (existing is not null)
            throw new InvalidOperationException($"Quyền '{req.Code}' đã tồn tại.");

        var perm = new PermissionEntity(req.Code, req.Name, req.Module, req.Description);
        await _permRepo.AddAsync(perm, ct);
        await _uow.CommitChangesAsync(ct);

        return new PermissionDto(perm.Id, perm.Code, perm.Name, perm.Module, perm.Description);
    }
}
