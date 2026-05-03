using EnglishPartner.Modules.Identity.Application.Abstractions;
using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;
using EnglishPartner.SharedKernel.Application.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.ResetPassword;

internal sealed class ResetPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IDistributedCache cache,
    ILogger<ResetPasswordCommandHandler> logger) : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email.ToLowerInvariant(), cancellationToken);
        if (user is null)
            return Result.Failure(IdentityErrors.User.NotFound);

        var cacheKey = CacheKeys.Identity.PasswordReset(user.Id);
        var storedCode = await cache.GetStringAsync(cacheKey, cancellationToken);

        if (storedCode is null || storedCode != request.Code)
            return Result.Failure(IdentityErrors.PasswordReset.InvalidToken);

        // Change password
        var newHash = passwordHasher.Hash(request.NewPassword);
        user.ChangePassword(newHash);

        // Invalidate the code (one-time use)
        await cache.RemoveAsync(cacheKey, cancellationToken);

        logger.LogInformation("ResetPassword: password changed for UserId {UserId}", user.Id);
        return Result.Success();
    }
}
