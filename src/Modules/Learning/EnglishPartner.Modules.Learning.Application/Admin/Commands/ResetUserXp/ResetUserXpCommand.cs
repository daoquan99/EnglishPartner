using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Learning.Application.Admin.Commands.ResetUserXp;

public sealed record ResetUserXpCommand(Guid UserId) : ICommand;
