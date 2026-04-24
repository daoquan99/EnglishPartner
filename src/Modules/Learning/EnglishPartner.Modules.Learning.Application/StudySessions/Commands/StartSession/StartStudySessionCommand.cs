using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Learning.Application.StudySessions.Commands.StartSession;

public sealed record StartStudySessionCommand(
    string SessionTitle,
    int TotalItems) : ICommand<Guid>;
