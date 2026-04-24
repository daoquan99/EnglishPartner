using EnglishPartner.Modules.Identity.Domain.Roles.Repositories;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Admin.Users.Queries.GetUsers;

internal sealed class GetUsersQueryHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository) : IQueryHandler<GetUsersQuery, GetUsersResponse>
{
    public async Task<Result<GetUsersResponse>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await userRepository.GetPagedAsync(
            request.Search,
            request.Role,
            request.IsActive,
            request.Page,
            request.PageSize,
            cancellationToken);

        // Batch role lookup: collect all unique roleIds → 1 query → build lookup dictionary
        var allRoleIds = items
            .SelectMany(u => u.UserRoles.Select(ur => ur.RoleId))
            .Distinct()
            .ToList();

        // Use GetAllAsync scoped to relevant roles via role entity
        var allRoles = await roleRepository.GetAllAsync(cancellationToken);
        var roleLookup = allRoles
            .Where(r => allRoleIds.Contains(r.Id))
            .ToDictionary(r => r.Id, r => r.Name);

        var dtos = items.Select(u => new UserSummaryDto(
            u.Id,
            u.Email,
            u.DisplayName,
            u.IsActive,
            u.IsOnboardingCompleted,
            u.UserRoles.Select(ur => new RoleSummaryDto(ur.RoleId, roleLookup.GetValueOrDefault(ur.RoleId, "Unknown"))).ToArray(),
            u.CreatedAt)).ToList();

        return new GetUsersResponse(dtos, total, request.Page, request.PageSize);
    }
}
