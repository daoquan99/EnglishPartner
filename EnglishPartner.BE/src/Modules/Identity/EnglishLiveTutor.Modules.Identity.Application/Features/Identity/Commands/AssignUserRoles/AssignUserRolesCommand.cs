using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.AssignUserRoles;

public sealed record AssignUserRolesCommand(
    Guid UserId,
    AssignUserRolesRequest Request
) : ICommand<UserProfileDto>;
