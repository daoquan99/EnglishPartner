using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Roles;
using EnglishPartner.Modules.Identity.Domain.Roles.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Admin.Roles.Commands.CreateRole;

internal sealed class CreateRoleCommandHandler(IRoleRepository roleRepository)
    : ICommandHandler<CreateRoleCommand, CreateRoleResponse>
{
    public async Task<Result<CreateRoleResponse>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        if (await roleRepository.ExistsByNameAsync(request.Name, cancellationToken))
            return Result.Failure<CreateRoleResponse>(IdentityErrors.Role.NameAlreadyExists);

        var role = Role.Create(request.Name, request.Description);
        roleRepository.Add(role);

        return Result.Success(new CreateRoleResponse(role.Id, role.Name, role.Description));
    }
}
