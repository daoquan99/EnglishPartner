using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Roles.Repositories;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Admin.Users.Commands.ManageUserRole;

internal sealed class ManageUserRoleCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository) : ICommandHandler<ManageUserRoleCommand>
{
    public async Task<Result> Handle(ManageUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(IdentityErrors.User.NotFoundById(request.UserId));

        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure(IdentityErrors.Role.NotFound);

        if (request.Operation.Equals("Assign", StringComparison.OrdinalIgnoreCase))
        {
            if (user.UserRoles.Any(ur => ur.RoleId == request.RoleId))
                return Result.Failure(new Error("Identity.User.RoleAlreadyAssigned",
                    $"User already has role '{role.Name}'."));
            user.AssignRole(request.RoleId);
        }
        else if (request.Operation.Equals("Revoke", StringComparison.OrdinalIgnoreCase))
        {
            user.RemoveRole(request.RoleId);
        }
        else
        {
            return Result.Failure(new Error("Identity.User.InvalidOperation",
                "Operation must be 'Assign' or 'Revoke'."));
        }

        return Result.Success();
    }
}
