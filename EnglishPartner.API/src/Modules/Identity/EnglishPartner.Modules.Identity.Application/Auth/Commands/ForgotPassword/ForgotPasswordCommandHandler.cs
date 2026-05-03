using System.Security.Cryptography;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Email;
using EnglishPartner.SharedKernel.Results;
using EnglishPartner.SharedKernel.Application.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.ForgotPassword;

internal sealed class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IDistributedCache cache,
    IEmailSender emailSender,
    ILogger<ForgotPasswordCommandHandler> logger) : ICommandHandler<ForgotPasswordCommand>
{
    private static readonly TimeSpan TokenExpiration = CacheTtl.Identity.PasswordResetToken;

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email.ToLowerInvariant(), cancellationToken);

        // Always return success to prevent email enumeration attacks
        if (user is null)
        {
            logger.LogWarning("ForgotPassword: no user found for email {Email}", request.Email);
            return Result.Success();
        }

        // Generate 6-digit OTP code
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        // Store in Redis with TTL
        var cacheKey = CacheKeys.Identity.PasswordReset(user.Id);
        await cache.SetStringAsync(cacheKey, code, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TokenExpiration
        }, cancellationToken);

        // Send email
        var html = $"""
            <h2>Password Reset</h2>
            <p>Hi {user.DisplayName},</p>
            <p>Your password reset code is:</p>
            <h1 style="letter-spacing:8px;font-size:36px;text-align:center;color:#00d4b0;">{code}</h1>
            <p>This code expires in {TokenExpiration.TotalMinutes} minutes.</p>
            <p>If you didn't request this, please ignore this email.</p>
            <p style="color:#888;font-size:12px;">— EnglishPartner</p>
            """;

        await emailSender.SendAsync(new EmailMessage(
            To: user.Email,
            Subject: "EnglishPartner — Password Reset Code",
            HtmlBody: html,
            PlainTextBody: $"Your password reset code is: {code}. It expires in {TokenExpiration.TotalMinutes} minutes."
        ), cancellationToken);

        logger.LogInformation("ForgotPassword: reset code sent to UserId {UserId}", user.Id);
        return Result.Success();
    }
}
