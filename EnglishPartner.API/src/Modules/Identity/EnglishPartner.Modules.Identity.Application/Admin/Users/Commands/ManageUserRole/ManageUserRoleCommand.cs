using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Identity.Application.Admin.Users.Commands.ManageUserRole;

public sealed record ManageUserRoleCommand(
    Guid   UserId,
    Guid   RoleId,
    string Operation   // "Assign" | "Revoke"
) : ICommand;
