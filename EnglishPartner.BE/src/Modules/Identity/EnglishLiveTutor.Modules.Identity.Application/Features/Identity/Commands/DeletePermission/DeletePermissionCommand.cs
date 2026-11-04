using EnglishLiveTutor.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.DeletePermission;

public sealed record DeletePermissionCommand(Guid PermissionId) : ICommand<bool>;
