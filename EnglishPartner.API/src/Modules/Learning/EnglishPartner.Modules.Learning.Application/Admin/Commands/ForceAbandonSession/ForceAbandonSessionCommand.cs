using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Learning.Application.Admin.Commands.ForceAbandonSession;

public sealed record ForceAbandonSessionCommand(Guid SessionId) : ICommand;
