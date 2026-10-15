using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity.Repositories;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.Login;

public sealed record LoginCommand(
    LoginRequest Request,
    string? IpAddress = null
) : ICommand<AuthResponseDto>;
