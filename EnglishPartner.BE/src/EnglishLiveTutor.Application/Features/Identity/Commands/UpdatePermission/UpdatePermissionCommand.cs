using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.UpdatePermission;

public sealed record UpdatePermissionCommand(Guid PermissionId, AdminUpdatePermissionRequest Request) : ICommand<PermissionDto?>;
