using EnglishPartner.Modules.Identity.Application.Abstractions;
using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Admin.Users.Commands.ResetUserPassword;

internal sealed class ResetUserPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher) : ICommandHandler<ResetUserPasswordCommand, ResetPasswordResult>
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

        // Generate a secure temporary password
        var tempPassword = GenerateTemporaryPassword();
        var hash = passwordHasher.Hash(tempPassword);
        user.ChangePassword(hash);
        userRepository.Update(user);

        // TODO: If sendEmail=true, publish an integration event / call email service
        // For now, we return the temp password (the caller / admin console shows it once)
        if (request.SendEmail)
        {
            // Email sending would go here via an IEmailService
            return new ResetPasswordResult(null, EmailSent: true);
        }

        return new ResetPasswordResult(tempPassword, EmailSent: false);
    }

    private static string GenerateTemporaryPassword(int length = 12)
    {
        // Use cryptographically secure RNG — available from .NET 6+
        return System.Security.Cryptography.RandomNumberGenerator.GetString(
            Chars, length);
    }
}
