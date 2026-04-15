using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Identity.Application.Admin.Users.Commands.ResetUserPassword;

public sealed record ResetUserPasswordCommand(Guid UserId, bool SendEmail) : ICommand<ResetPasswordResult>;

public sealed record ResetPasswordResult(string? TemporaryPassword, bool EmailSent);
