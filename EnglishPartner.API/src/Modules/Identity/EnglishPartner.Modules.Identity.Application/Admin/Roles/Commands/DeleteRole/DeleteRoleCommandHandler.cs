using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Roles.Repositories;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Admin.Roles.Commands.DeleteRole;

internal sealed class DeleteRoleCommandHandler(
    IRoleRepository roleRepository,
    IUserRepository userRepository)
    : ICommandHandler<DeleteRoleCommand>
{
    public async Task<Result> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure(IdentityErrors.Role.NotFound);

        var hasUsers = await userRepository.AnyWithRoleAsync(request.RoleId, cancellationToken);
        if (hasUsers)
            return Result.Failure(IdentityErrors.Role.HasUsers);

        roleRepository.Delete(role);
        return Result.Success();
    }
}
