using EnglishPartner.Modules.Learning.Domain.StudySessions.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.StudySessions.Commands.PingSession;

internal sealed class PingSessionCommandHandler(
    IStudySessionRepository sessionRepository) : ICommandHandler<PingSessionCommand>
{
    public async Task<Result> Handle(PingSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session is null || session.UserId != request.UserId) 
            return Result.Failure(new Error("StudySession.NotFound", "Session not found."));

        session.Ping(DateTime.UtcNow);
        sessionRepository.Update(session);
        return Result.Success();
    }
}
