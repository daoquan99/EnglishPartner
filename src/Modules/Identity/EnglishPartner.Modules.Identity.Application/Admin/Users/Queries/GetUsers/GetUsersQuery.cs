using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Identity.Application.Admin.Users.Queries.GetUsers;

public sealed record GetUsersQuery(
    string? Search,
    string? Role,
    bool?   IsActive,
    int     Page,
    int     PageSize) : IQuery<GetUsersResponse>;

public sealed record GetUsersResponse(
    IReadOnlyList<UserSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record UserSummaryDto(
    Guid     Id,
    string   Email,
    string   DisplayName,
    bool     IsActive,
    bool     IsOnboardingCompleted,
    string[] Roles,
    DateTime CreatedAt);
