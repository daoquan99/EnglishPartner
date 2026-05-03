using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Identity.Application.Admin.Roles.Commands.CreateRole;

public sealed record CreateRoleCommand(string Name, string? Description) : ICommand<CreateRoleResponse>;

public sealed record CreateRoleResponse(Guid Id, string Name, string? Description);
