using EnglishPartner.Modules.Learning.Domain.StudySessions.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.StudySessions.Commands.SubmitSession;

internal sealed class SubmitStudySessionResultCommandHandler(
    IStudySessionRepository sessionRepository) : ICommandHandler<SubmitStudySessionResultCommand>
{
    public async Task<Result> Handle(SubmitStudySessionResultCommand request, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session is null) return Result.Failure(new Error("StudySession.NotFound", "Session not found."));

        session.Complete(request.Score);
        sessionRepository.Update(session);
        return Result.Success();
    }
}
