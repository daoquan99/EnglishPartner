using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
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
        string token = command.Request.RefreshToken?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var user = await _userRepo.GetByRefreshTokenAsync(token, ct);
        if (user is null)
            return false;

        user.RevokeRefreshToken(token, DateTimeOffset.UtcNow, command.Request.Reason);
        await _userRepo.SaveChangesAsync(ct);
        return true;
    }
}
