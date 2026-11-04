using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Identity.Repositories;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    RegisterUserRequest Request,
    string? IpAddress = null
) : ICommand<AuthResponseDto>;
