using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.ResetPassword;

/// <summary>Resets password using the OTP code from forgot-password email.</summary>
public sealed record ResetPasswordCommand(string Email, string Code, string NewPassword) : ICommand;
