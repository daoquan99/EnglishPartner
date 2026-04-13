using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.Logout;

internal sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokenRepository) : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await refreshTokenRepository.RevokeAllByUserIdAsync(request.UserId, cancellationToken);
        return Result.Success();
    }
}
