using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.ForgotPassword;

/// <summary>User requests a password reset link via email.</summary>
public sealed record ForgotPasswordCommand(string Email) : ICommand;
