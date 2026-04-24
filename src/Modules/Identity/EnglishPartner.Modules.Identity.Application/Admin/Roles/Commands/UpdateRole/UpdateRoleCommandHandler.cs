using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Roles.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Admin.Roles.Commands.UpdateRole;

internal sealed class UpdateRoleCommandHandler(IRoleRepository roleRepository)
    : ICommandHandler<UpdateRoleCommand>
{
    public async Task<Result> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure(IdentityErrors.Role.NotFound);

        if (!role.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)
            && await roleRepository.ExistsByNameAsync(request.Name, cancellationToken))
            return Result.Failure(IdentityErrors.Role.NameAlreadyExists);

        role.Update(request.Name, request.Description);
        roleRepository.Update(role);

        return Result.Success();
    }
}
