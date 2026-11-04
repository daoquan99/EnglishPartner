using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity.Repositories;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.RevokeRefreshToken;

public sealed record RevokeRefreshTokenCommand(
    RevokeTokenRequest Request
) : ICommand<bool>;
