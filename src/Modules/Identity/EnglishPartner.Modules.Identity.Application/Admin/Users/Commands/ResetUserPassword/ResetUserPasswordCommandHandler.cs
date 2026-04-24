using EnglishPartner.Modules.Identity.Application.Abstractions;
using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Email;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Admin.Users.Commands.ResetUserPassword;

internal sealed class ResetUserPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender) : ICommandHandler<ResetUserPasswordCommand, ResetPasswordResult>
{
    // Characters for generated temporary passwords
    private const string Chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$";

    public async Task<Result<ResetPasswordResult>> Handle(
        ResetUserPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<ResetPasswordResult>(IdentityErrors.User.NotFoundById(request.UserId));

        var tempPassword = GenerateTemporaryPassword();
        var hash = passwordHasher.Hash(tempPassword);
        user.ChangePassword(hash);
        userRepository.Update(user);

        if (request.SendEmail)
        {
            var html = $"""
                <h2>Password Reset</h2>
                <p>Hi {user.DisplayName},</p>
                <p>Your password has been reset by an administrator.</p>
                <p>Your temporary password is:</p>
                <h1 style="letter-spacing:4px;font-size:24px;text-align:center;color:#00d4b0;">{tempPassword}</h1>
                <p>Please login and change your password immediately.</p>
                <p style="color:#888;font-size:12px;">— EnglishPartner</p>
                """;

            await emailSender.SendAsync(new EmailMessage(
                To: user.Email,
                Subject: "EnglishPartner — Your Password Has Been Reset",
                HtmlBody: html,
                PlainTextBody: $"Your temporary password is: {tempPassword}. Please login and change it immediately."
            ), cancellationToken);

            return new ResetPasswordResult(null, EmailSent: true);
        }

        return new ResetPasswordResult(tempPassword, EmailSent: false);
    }

    private static string GenerateTemporaryPassword(int length = 12)
    {
        return System.Security.Cryptography.RandomNumberGenerator.GetString(
            Chars, length);
    }
}
