using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Learning.Application.StudySessions.Commands.StartSession;

public sealed record StartStudySessionCommand(
    Guid UserId,
    string SessionTitle,
    int TotalItems) : ICommand<Guid>;
