using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Queries.ListPermissions;

public sealed record ListPermissionsQuery(
    string? Search = null,
    int? Page = null,
    int? PageSize = null
) : IQuery<PagedResultDto<PermissionDto>>;
