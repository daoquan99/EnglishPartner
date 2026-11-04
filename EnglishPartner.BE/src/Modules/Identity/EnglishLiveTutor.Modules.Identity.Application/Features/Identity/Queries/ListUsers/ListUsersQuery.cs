using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Queries.ListUsers;

public sealed record ListUsersQuery(
    string? Search = null,
    int? Page = null,
    int? PageSize = null
) : IQuery<PagedResultDto<UserProfileDto>>;
