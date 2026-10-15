using EnglishLiveTutor.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.DeleteRole;

public sealed record DeleteRoleCommand(Guid RoleId) : ICommand<bool>;
