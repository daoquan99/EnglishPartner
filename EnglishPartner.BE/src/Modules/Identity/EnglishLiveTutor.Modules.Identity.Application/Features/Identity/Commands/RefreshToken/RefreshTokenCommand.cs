using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity.Repositories;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    RefreshTokenRequest Request,
    string? IpAddress = null
) : ICommand<AuthResponseDto>;
