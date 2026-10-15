using EnglishLiveTutor.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.DeleteUser;

public sealed record DeleteUserCommand(Guid UserId) : ICommand<bool>;
