using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.UpdateRolePermissions;

public sealed record UpdateRolePermissionsCommand(
    Guid RoleId,
    UpdateRolePermissionsRequest Request
) : ICommand<RoleDto>;
