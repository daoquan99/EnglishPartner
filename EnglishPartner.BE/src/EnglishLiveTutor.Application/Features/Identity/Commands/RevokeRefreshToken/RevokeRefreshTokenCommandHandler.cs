using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity.Entities;
using EnglishLiveTutor.Domain.Identity.Repositories;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.RevokeRefreshToken;

public sealed class RevokeRefreshTokenCommandHandler : ICommandHandler<RevokeRefreshTokenCommand, bool>
{
    private readonly IUserRepository _userRepo;

    public RevokeRefreshTokenCommandHandler(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<bool> HandleAsync(RevokeRefreshTokenCommand command, CancellationToken ct = default)
    {
        string rawToken = command.Request.RefreshToken?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rawToken))
            return false;

        string tokenHash = RefreshTokenEntity.HashToken(rawToken);
        var user = await _userRepo.GetByRefreshTokenAsync(tokenHash, ct);
        if (user is null)
            return false;

        user.RevokeRefreshToken(tokenHash, DateTimeOffset.UtcNow, command.Request.Reason ?? "Revoked by user");
        await _userRepo.SaveChangesAsync(ct);
        return true;
    }
}
