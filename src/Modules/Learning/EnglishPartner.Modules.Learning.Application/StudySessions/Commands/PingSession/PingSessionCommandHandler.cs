using EnglishPartner.Modules.Learning.Domain.StudySessions.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.StudySessions.Commands.PingSession;

internal sealed class PingSessionCommandHandler(
    IStudySessionRepository sessionRepository,
    TimeProvider timeProvider,
    ICurrentUserService currentUser) : ICommandHandler<PingSessionCommand>
{
    public async Task<Result> Handle(PingSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session is null || session.UserId != currentUser.UserId!.Value)
            return Result.Failure(CommonErrors.NotFound("StudySession", request.SessionId));

        session.Ping(timeProvider.GetUtcNow().UtcDateTime);
        sessionRepository.Update(session);
        return Result.Success();
    }
}
