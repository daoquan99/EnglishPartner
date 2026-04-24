using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Learning.Application.StudySessions.Commands.PingSession;

public sealed record PingSessionCommand(Guid SessionId) : ICommand;
