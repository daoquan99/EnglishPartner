using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Learning.Application.Admin.Commands.ResetUserMasteries;

public sealed record ResetUserMasteriesCommand(Guid UserId) : ICommand;
