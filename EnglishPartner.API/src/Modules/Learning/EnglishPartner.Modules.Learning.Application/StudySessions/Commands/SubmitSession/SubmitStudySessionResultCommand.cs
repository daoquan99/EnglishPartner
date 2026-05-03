using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Learning.Application.StudySessions.Commands.SubmitSession;

public sealed record SubmitStudySessionResultCommand(
    Guid SessionId,
    int Score) : ICommand;
